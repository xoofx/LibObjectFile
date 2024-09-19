﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using static System.Collections.Specialized.BitVector32;

namespace LibObjectFile.PE;

partial class PEFile
{
    /// <summary>
    /// Reads an <see cref="PEFile"/> from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read PE file from</param>
    /// <param name="options">The options for the reader</param>
    /// <returns>An instance of <see cref="PEFile"/> if the read was successful.</returns>
    public static PEFile Read(Stream stream, PEImageReaderOptions? options = null)
    {
        if (!TryRead(stream, out var objectFile, out var diagnostics, options))
        {
            throw new ObjectFileException($"Unexpected error while reading PE file", diagnostics);
        }
        return objectFile;
    }

    /// <summary>
    /// Tries to read an <see cref="PEFile"/> from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read PE file from</param>
    /// <param name="peFile"> instance of <see cref="PEFile"/> if the read was successful.</param>
    /// <param name="diagnostics">A <see cref="DiagnosticBag"/> instance</param>
    /// <param name="options">The options for the reader</param>
    /// <returns><c>true</c> An instance of <see cref="PEFile"/> if the read was successful.</returns>
    public static bool TryRead(Stream stream, [NotNullWhen(true)] out PEFile? peFile, [NotNullWhen(false)] out DiagnosticBag? diagnostics, PEImageReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new PEImageReaderOptions();
        peFile = new PEFile(false);
        var reader = new PEImageReader(peFile, stream, options);
        diagnostics = reader.Diagnostics;

        peFile.Read(reader);

        return !reader.Diagnostics.HasErrors;
    }

    public override void Read(PEImageReader reader)
    {
        Debug.Assert(Unsafe.SizeOf<ImageDosHeader>() == 64);

        var diagnostics = reader.Diagnostics;
        int read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref DosHeader, 1)));
        if (read != Unsafe.SizeOf<ImageDosHeader>())
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosHeaderSize, "Invalid DOS header");
            return;
        }

        if (DosHeader.Magic != ImageDosMagic.DOS)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosHeaderMagic, "Invalid DOS header");
            return;
        }

        // Read the DOS stub
        var dosStubSize = DosHeader.SizeOfParagraphsHeader * 16;
        if (dosStubSize > 0)
        {
            var dosStub = new byte[dosStubSize];
            read = reader.Read(dosStub);
            if (read != dosStubSize)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosStubSize, "Invalid DOS stub");
            }

            _dosStub = dosStub;
        }
        else
        {
            _dosStub = [];
        }

        // Read any DOS stub extra data (e.g Rich)
        if (DosHeader.FileAddressPEHeader > read)
        {
            _dosStubExtra = reader.ReadAsStream((ulong)(DosHeader.FileAddressPEHeader - read));
        }

        // Read the PE signature
        reader.Stream.Seek(DosHeader.FileAddressPEHeader, SeekOrigin.Begin);

        var signature = default(ImagePESignature);
        read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref signature, 1)));
        if (read != sizeof(ImagePESignature))
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidPESignature, "Invalid PE signature");
            return;
        }

        if (signature != ImagePESignature.PE)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidPESignature, $"Invalid PE signature 0x{(uint)signature:X8}");
            return;
        }

        // Read the COFF header
        Debug.Assert(Unsafe.SizeOf<ImageCoffHeader>() == 20);
        var coffHeader = default(ImageCoffHeader);
        read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref coffHeader, 1)));
        if (read != Unsafe.SizeOf<ImageCoffHeader>())
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidCoffHeaderSize, "Invalid COFF header");
            return;
        }
        CoffHeader = coffHeader;

        var tempArray = ArrayPool<byte>.Shared.Rent(CoffHeader.SizeOfOptionalHeader);
        try
        {
            var optionalHeader = new Span<byte>(tempArray, 0, CoffHeader.SizeOfOptionalHeader);
            read = reader.Read(optionalHeader);
            if (read != CoffHeader.SizeOfOptionalHeader)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidOptionalHeaderSize, "Invalid optional header");
                return;
            }

            var magic = MemoryMarshal.Cast<byte, ImageOptionalHeaderMagic>(optionalHeader.Slice(0, 2))[0];

            Debug.Assert(Unsafe.SizeOf<RawImageOptionalHeader32>() == 224);
            Debug.Assert(Unsafe.SizeOf<RawImageOptionalHeader64>() == 240);

            // Process known PE32/PE32+ headers
            if (magic == ImageOptionalHeaderMagic.PE32)
            {
                var optionalHeader32 = new RawImageOptionalHeader32();
                var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref optionalHeader32, 1));
                if (span.Length > CoffHeader.SizeOfOptionalHeader)
                {
                    span = span.Slice(0, CoffHeader.SizeOfOptionalHeader);
                }

                optionalHeader.CopyTo(span);

                OptionalHeader.OptionalHeaderCommonPart1 = optionalHeader32.Common;
                OptionalHeader.OptionalHeaderBase32 = optionalHeader32.Base32;
                OptionalHeader.OptionalHeaderCommonPart2 = optionalHeader32.Common2;
                OptionalHeader.OptionalHeaderSize32 = optionalHeader32.Size32;
                OptionalHeader.OptionalHeaderCommonPart3 = optionalHeader32.Common3;
            }
            else if (magic == ImageOptionalHeaderMagic.PE32Plus)
            {
                var optionalHeader64 = new RawImageOptionalHeader64();
                // Skip 2 bytes as we read already the magic number
                var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref optionalHeader64, 1));
                if (span.Length > CoffHeader.SizeOfOptionalHeader)
                {
                    span = span.Slice(0, CoffHeader.SizeOfOptionalHeader);
                }

                optionalHeader.CopyTo(span);

                OptionalHeader.OptionalHeaderCommonPart1 = optionalHeader64.Common;
                OptionalHeader.OptionalHeaderBase64 = optionalHeader64.Base64;
                OptionalHeader.OptionalHeaderCommonPart2 = optionalHeader64.Common2;
                OptionalHeader.OptionalHeaderSize64 = optionalHeader64.Size64;
                OptionalHeader.OptionalHeaderCommonPart3 = optionalHeader64.Common3;
            }
            else
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidOptionalHeaderMagic, $"Invalid optional header PE magic 0x{(uint)magic:X8}");
                return;
            }

            // Read sections
            ArrayPool<byte>.Shared.Return(tempArray);
            Debug.Assert(Unsafe.SizeOf<RawImageSectionHeader>() == 40);
            var sizeOfSections = CoffHeader.NumberOfSections * Unsafe.SizeOf<RawImageSectionHeader>();
            tempArray = ArrayPool<byte>.Shared.Rent(sizeOfSections);
            var spanSections = new Span<byte>(tempArray, 0, sizeOfSections);
            read = reader.Read(spanSections);

            var sectionHeaders = MemoryMarshal.Cast<byte, RawImageSectionHeader>(spanSections);
            if (read != spanSections.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidSectionHeadersSize, "Invalid section headers");
            }

            InitializeSections(reader, sectionHeaders);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    private void InitializeSections(PEImageReader imageReader, ReadOnlySpan<RawImageSectionHeader> headers)
    {
        _sections.Clear();

        // Create sections
        foreach (var section in headers)
        {
            // We don't validate the name
            var peSection = new PESection( new PESectionName(section.NameAsString, false), section.VirtualAddress, section.VirtualSize)
            {
                Position = section.PointerToRawData,
                Size = section.SizeOfRawData,
                Characteristics = section.Characteristics,
            };
            _sections.Add(peSection);
        }

        var sectionDatas = new List<PESectionData>();

        // Create directories and find the section for each directory
        var maxNumberOfDirectory = (int)Math.Min(OptionalHeader.NumberOfRvaAndSizes, 15);
        Span<ImageDataDirectory> dataDirectories = OptionalHeader.DataDirectory;
        for (int i = 0; i < maxNumberOfDirectory && i < dataDirectories.Length; i++)
        {
            var directoryEntry = dataDirectories[i];
            if (directoryEntry.VirtualAddress == 0 || directoryEntry.Size == 0)
            {
                continue;
            }

            if (!TryFindSection(directoryEntry.VirtualAddress, directoryEntry.Size, out var peSection))
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidDataDirectorySection, $"Unable to find the section for the DataDirectory at virtual address {directoryEntry.VirtualAddress}, size {directoryEntry.Size}");
                continue;
            }

            var offsetInSection = directoryEntry.VirtualAddress - peSection.VirtualAddress;
            var directory = PEDirectory.Create((ImageDataDirectoryKind)i);
            directory.Position = peSection.Position + offsetInSection;
            directory.Size = directoryEntry.Size;

            Directories.Set(directory.Kind, directory);

            sectionDatas.Add(directory);
        }

        // Special case for Import Directory, we preload it to load ImportLookupTable and ImportAddressTable
        // so that we can attach them below to the right section data before creating the remaining interleaved streams
        var importDirectory = Directories.Import;
        if (importDirectory != null)
        {
            // Read Import Directory Entries first
            importDirectory.Read(imageReader);

            var importAddressTableDirectory = Directories.ImportAddressTable;
            if (importAddressTableDirectory is null)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_ImportAddressTableNotFound, "Unable to find the Import Address Table");
                return;
            }

            // Register the import address table directory
            foreach (var entry in importDirectory.Entries)
            {
                importAddressTableDirectory.Content.Add(entry.ImportAddressTable);
                sectionDatas.Add(entry.ImportLookupTable);
            }
        }
        
        // Attach all the section data we have created (directories, import tables) to the sections
        foreach (var vObj in sectionDatas)
        {
            int sectionIndex = 0;
            for (; sectionIndex < _sections.Count; sectionIndex++)
            {
                var section = _sections[sectionIndex];
                if (section.Contains(vObj.Position))
                {
                    break;
                }
            }

            sectionIndex = Math.Min(sectionIndex, Math.Max(0, _sections.Count - 1));
            var peSection = _sections[sectionIndex];
            
            // Insert the directory at the right position in the section
            int sectionDataIndex = 0;
            var dataParts = peSection.DataParts;
            for (; sectionDataIndex < dataParts.Count; sectionDataIndex++)
            {
                var sectionData = dataParts[sectionDataIndex];
                if (vObj.Position < sectionData.Position)
                {
                    break;
                }
            }

            dataParts.Insert(sectionDataIndex, vObj);
        }

        // Create Stream data sections for remaining data per section based on directories already loaded
        foreach (var section in _sections)
        {
            var sectionPosition = section.Position;

            var dataParts = section.DataParts;
            for (var i = 0; i < dataParts.Count; i++)
            {
                var data = dataParts[i];
                if (data.Position != sectionPosition)
                {
                    var sectionData = new PEStreamSectionData()
                    {
                        Position = sectionPosition,
                    };
                    var size = data.Position - sectionPosition;
                    imageReader.Position = sectionPosition;
                    sectionData.Stream = imageReader.ReadAsStream(size);

                    dataParts.Insert(i, sectionData);
                    sectionPosition = data.Position;
                    i++;
                }

                sectionPosition += data.Size;
            }

            if (sectionPosition < section.Position + section.Size)
            {
                var sectionData = new PEStreamSectionData()
                {
                    Position = sectionPosition,
                };
                var size = section.Position + section.Size - sectionPosition;
                imageReader.Position = sectionPosition;
                sectionData.Stream = imageReader.ReadAsStream(size);

                dataParts.Add(sectionData);
            }

            //for (var i = 0; i < section.DataParts.Count; i++)
            //{
            //    var sectionData = section.DataParts[i];
            //    Console.WriteLine($"section: {section.Name} {sectionData}");
            //}
        }

        // Post fix the ImportLookupTable and ImportAddressTable
        // To attach proper links to the actual streams
        if (importDirectory is not null)
        {
            foreach (var entry in importDirectory.Entries)
            {
                entry.ImportAddressTable.FunctionTable.ResolveSectionDataLinks(this, imageReader.Diagnostics);
                entry.ImportLookupTable.FunctionTable.ResolveSectionDataLinks(this, imageReader.Diagnostics);
            }
        }

        // Read directories
        // TODO: Read all directories
        Directories.BaseRelocation?.Read(imageReader);
    }
}