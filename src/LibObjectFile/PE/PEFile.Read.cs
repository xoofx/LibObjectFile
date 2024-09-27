// Copyright (c) Alexandre Mutel. All rights reserved.
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
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

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

        read = (int)reader.Position;
        // Read any DOS stub extra data (e.g Rich)
        if (DosHeader.FileAddressPEHeader > read)
        {
            _dosStubExtra = reader.ReadAsStream((ulong)(DosHeader.FileAddressPEHeader - read));
        }

        // Read the PE signature
        if (reader.Position != DosHeader.FileAddressPEHeader)
        {
            reader.Position = DosHeader.FileAddressPEHeader;
        }

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

            // Read all sections and directories
            ReadSectionsAndDirectories(reader, sectionHeaders);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    private void ReadSectionsAndDirectories(PEImageReader reader, ReadOnlySpan<RawImageSectionHeader> sectionHeaders)
    {
        _sections.Clear();

        uint positionAfterHeaders = (uint)reader.Position;
        uint positionFirstSection = positionAfterHeaders;

        // Load any data stored before the sections
        if (sectionHeaders.Length > 0)
        {
            positionFirstSection = sectionHeaders[0].PointerToRawData;
        }

        uint positionAfterLastSection = positionFirstSection;
        
        // Create sections
        foreach (var section in sectionHeaders)
        {
            // We don't validate the name
            var peSection = new PESection( new PESectionName(section.NameAsString, false), section.RVA, section.VirtualSize)
            {
                Position = section.PointerToRawData,
                // Use the exact size of the section on disk if virtual size is smaller
                // as these are considered as padding between sections
                Size = section.VirtualSize < section.SizeOfRawData ? section.VirtualSize : section.SizeOfRawData,
                Characteristics = section.Characteristics,
            };

            var positionAfterSection = section.PointerToRawData + section.SizeOfRawData;
            if (positionAfterSection > positionAfterLastSection)
            {
                positionAfterLastSection = positionAfterSection;
            }

            _sections.Add(peSection);
        }

        // A list that contains all the section data that we have created (e.g. from directories, import tables...)
        var sectionDataList = new List<PESectionData>();
        var extraDataList = new List<PEExtraData>();

        // Create directories and find the section for each directory
        var maxNumberOfDirectory = (int)Math.Min(OptionalHeader.NumberOfRvaAndSizes, 15);
        Span<ImageDataDirectory> dataDirectories = OptionalHeader.DataDirectory;
        for (int i = 0; i < maxNumberOfDirectory && i < dataDirectories.Length; i++)
        {
            var directoryEntry = dataDirectories[i];
            if (directoryEntry.RVA == 0 || directoryEntry.Size == 0)
            {
                continue;
            }

            if (!TryFindSection(directoryEntry.RVA, directoryEntry.Size, out var peSection))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidDataDirectorySection, $"Unable to find the section for the DataDirectory at virtual address {directoryEntry.RVA}, size {directoryEntry.Size}");
                continue;
            }

            var kind = (PEDataDirectoryKind)i;
            if (kind == PEDataDirectoryKind.SecurityCertificate)
            {
                var directory = new PESecurityCertificateDirectory();
                
                // The PE certificate directory is a special case as it is not a standard directory. It doesn't use RVA but the position in the file
                directory.Position = (uint)directoryEntry.RVA;
                directory.Size = directoryEntry.Size;

                Directories.Set(directory);

                // The PESecurityDirectory is a special case as it doesn't use RVA but the position in the file. It belongs after the sections to the extra data
                extraDataList.Add(directory);
                
                directory.Read(reader);
            }
            else
            {
                var directory = PEDataDirectory.Create((PEDataDirectoryKind)i, IsPE32);
                var offsetInSection = directoryEntry.RVA - peSection.RVA;
                directory.Position = peSection.Position + offsetInSection;

                directory.Size = directoryEntry.Size;

                Directories.Set(directory.Kind, directory);

                sectionDataList.Add(directory);

                // Read the content of the directory
                directory.Read(reader);
            }

            if (reader.Diagnostics.HasErrors)
            {
                return;
            }
        }

        // Get all implicit section data from directories after registering directories
        foreach (var directory in Directories)
        {
            // Add all implicit data
            foreach (var implicitData in directory.CollectImplicitSectionDataList())
            {
                if (implicitData is PESectionData sectionData)
                {
                    sectionDataList.Add(sectionData);
                }
                else if (implicitData is PEExtraData extraData)
                {
                    extraDataList.Add(extraData);
                }
            }
        }

        // Process all extra data list
        foreach(var extraData in extraDataList)
        {
            if (extraData.Position < positionFirstSection)
            {
                ExtraDataBeforeSections.Add(extraData);
            }
            else if (extraData.Position >= positionAfterLastSection)
            {
                ExtraDataAfterSections.Add(extraData);
            }
            else
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExtraData, $"Extra data found in the middle of the sections at {extraData.Position}");
            }
        }
        
        // Attach all the section data we have created (from e.g. directories, import tables) to the sections
        foreach (var vObj in sectionDataList)
        {
            int sectionIndex = 0;
            bool sectionFound = false;
            for (; sectionIndex < _sections.Count; sectionIndex++)
            {
                var section = _sections[sectionIndex];
                if (section.Contains(vObj.Position))
                {
                    sectionFound = true;
                    break;
                }
            }

            if (!sectionFound)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Unable to find the section for {vObj} at position {vObj.Position}");
                return;
            }

            sectionIndex = Math.Min(sectionIndex, Math.Max(0, _sections.Count - 1));
            var peSection = _sections[sectionIndex];
            
            // Insert the directory at the right position in the section
            int sectionDataIndex = 0;
            var dataParts = peSection.Content;

            bool addedToDirectory = false;

            for (; sectionDataIndex < dataParts.Count; sectionDataIndex++)
            {
                var sectionData = dataParts[sectionDataIndex];
                if (vObj.Position < sectionData.Position)
                {
                    break;
                }
                else if (sectionData.Contains(vObj.Position, (uint)vObj.Size))
                {
                    if (sectionData is PEDataDirectory dataDirectory)
                    {
                        addedToDirectory = true;
                        dataDirectory.Content.Add(vObj);
                        break;
                    }
                    else
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid section data {sectionData} at position {sectionData.Position} contains {vObj} at position {vObj.Position}");
                        return;
                    }
                }
            }

            if (!addedToDirectory)
            {
                dataParts.Insert(sectionDataIndex, vObj);
            }
        }

        // Make sure that we have a proper stream for all directories
        foreach (var directory in Directories)
        {
            FillDirectoryWithStreams(reader, directory);
        }

        // Create all remaining extra data section data (before and after sections)
        FillExtraDataWithMissingStreams(reader, this, ExtraDataBeforeSections, positionAfterHeaders, positionFirstSection - positionAfterHeaders);
        FillExtraDataWithMissingStreams(reader, this, ExtraDataAfterSections, positionAfterLastSection, reader.Length - positionAfterLastSection);

        // Create Stream data sections for remaining data per section based on directories already loaded
        for (var i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            FillSectionDataWithMissingStreams(reader, section, section.Content, section.Position, section.Size);

            var previousSize = sectionHeaders[i].SizeOfRawData;

            section.UpdateLayout(reader);

            var newSize = section.Size;
            if (newSize != previousSize)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid size for section {section} at {section.Position} with size {section.Size} != {previousSize}");
                return;
            }
        }

        // Final binding of directories with the actual section data
        foreach (var directory in Directories)
        {
            directory.Bind(reader);
        }
        
        // Validate the VirtualAddress of the directories
        for (int i = 0; i < maxNumberOfDirectory && i < dataDirectories.Length; i++)
        {
            var directoryEntry = dataDirectories[i];
            if (directoryEntry.RVA == 0 || directoryEntry.Size == 0)
            {
                continue;
            }

            // We have the guarantee that the directory is not null
            var directory = Directories[(PEDataDirectoryKind)i]!;

            if (directory is PEDataDirectory peDataDirectory)
            {
                if (peDataDirectory.RVA != directoryEntry.RVA)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid virtual address for directory {peDataDirectory.Kind} at {peDataDirectory.RVA} != {directoryEntry.RVA}");
                }
            }
            else if (directory is PESecurityCertificateDirectory peSecurityDirectory)
            {
                if (peSecurityDirectory.Position != directoryEntry.RVA)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid position for security directory at {peSecurityDirectory.Position} != {directoryEntry.RVA}");
                }
            }
        }
    }

    /// <summary>
    /// For a list of section data (e.g. used by a section or a ImportAddressTableDirectory...), we need to fill any hole with the actual stream of original data from the image
    /// </summary>
    private static void FillSectionDataWithMissingStreams(PEImageReader imageReader, PEObjectBase container, ObjectList<PESectionData> dataParts, ulong startPosition, ulong totalSize)
    {
        var currentPosition = startPosition;

        // We are working on position, while the list is ordered by VirtualAddress

        var listOrderedByPosition = dataParts.UnsafeList;

        listOrderedByPosition.Sort((a, b) => a.Position.CompareTo(b.Position));
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            data.Index = i;
        }
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            if (currentPosition < data.Position)
            {
                var size = data.Position - currentPosition;
                imageReader.Position = currentPosition;

                var sectionData = new PEStreamSectionData(imageReader.ReadAsStream(size))
                {
                    Position = currentPosition,
                    Parent = container,
                };

                listOrderedByPosition.Insert(i, sectionData);
                currentPosition = data.Position;
                i++;
            }
            else if (currentPosition > data.Position)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid section data position {currentPosition} > {data.Position} in {container}");
                return;
            }

            currentPosition += data.Size;
        }

        if (currentPosition < startPosition + totalSize)
        {
            var size = startPosition + totalSize - currentPosition;
            imageReader.Position = currentPosition;
            var sectionData = new PEStreamSectionData(imageReader.ReadAsStream(size))
            {
                Position = currentPosition,
                Parent = container,
            };

            listOrderedByPosition.Add(sectionData);
        }
        else if (currentPosition > startPosition + totalSize)
        {
            imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid section data position {currentPosition} > {startPosition + totalSize} in {container}");
        }

        // Make sure to update the indices after inserting the missing streams
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            data.Index = i;
        }
    }
    
    private static void FillExtraDataWithMissingStreams(PEImageReader imageReader, PEObjectBase parent, ObjectList<PEExtraData> list, ulong extraPosition, ulong extraTotalSize)
    {
        var currentPosition = extraPosition;
        imageReader.Position = extraPosition;

        // We are working on position, while the list is ordered by VirtualAddress
        var listOrderedByPosition = list.UnsafeList;
        listOrderedByPosition.Sort((a, b) => a.Position.CompareTo(b.Position));
        
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            if (currentPosition < data.Position)
            {
                var size = data.Position - currentPosition;
                imageReader.Position = currentPosition;

                var sectionData = new PEStreamExtraData(imageReader.ReadAsStream(size))
                {
                    Position = currentPosition,
                    Parent = parent,
                };

                listOrderedByPosition.Insert(i, sectionData);
                currentPosition = data.Position;
                i++;
            }
            else if (currentPosition > data.Position)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid extra data position {currentPosition} > {data.Position}");
                return;
            }

            currentPosition += data.Size;
        }

        if (currentPosition < extraPosition + extraTotalSize)
        {
            var size = extraPosition + extraTotalSize - currentPosition;
            imageReader.Position = currentPosition;
            var sectionData = new PEStreamExtraData(imageReader.ReadAsStream(size))
            {
                Position = currentPosition,
                Parent = parent,
            };

            listOrderedByPosition.Add(sectionData);
        }
        else if (currentPosition > extraPosition + extraTotalSize)
        {
            imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid extra data position {currentPosition} > {extraPosition + extraTotalSize}");
        }

        // Make sure to update the indices after inserting the missing streams
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            data.Index = i;
        }
    }

    private static void FillDirectoryWithStreams(PEImageReader imageReader, PEDataDirectory directory)
    {
        FillSectionDataWithMissingStreams(imageReader, directory, directory.Content, directory.Position + directory.HeaderSize, directory.Size - directory.HeaderSize);
    }
}