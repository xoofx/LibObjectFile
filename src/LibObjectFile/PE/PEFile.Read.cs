// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.PE.Internal;

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

        peFile.ReadFromInternal(reader);

        return !reader.Diagnostics.HasErrors;
    }

    internal void ReadFromInternal(PEImageReader imageReader)
    {
        Debug.Assert(Unsafe.SizeOf<ImageDosHeader>() == 64);

        var diagnostics = imageReader.Diagnostics;
        int read = imageReader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref DosHeader, 1)));
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
            read = imageReader.Read(dosStub);
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
            _dosStubExtra = imageReader.ReadAsStream((ulong)(DosHeader.FileAddressPEHeader - read));
        }

        // Read the PE signature
        imageReader.Stream.Seek(DosHeader.FileAddressPEHeader, SeekOrigin.Begin);

        var signature = default(ImagePESignature);
        read = imageReader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref signature, 1)));
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
        read = imageReader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref coffHeader, 1)));
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
            read = imageReader.Read(optionalHeader);
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
            read = imageReader.Read(spanSections);

            var sectionHeaders = MemoryMarshal.Cast<byte, RawImageSectionHeader>(spanSections);
            if (read != spanSections.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidSectionHeadersSize, "Invalid section headers");
            }

            InitializeSections(sectionHeaders);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    private void InitializeSections(ReadOnlySpan<RawImageSectionHeader> headers)
    {
        _sections.Clear();
        foreach (var section in headers)
        {
            // We don't validate the name
            var peSection = new PESection(this, new PESectionName(section.NameAsString, false))
            {
                Offset = section.PointerToRawData,
                Size = section.SizeOfRawData,
                VirtualAddress = section.VirtualAddress,
                VirtualSize = section.VirtualSize,
                Characteristics = section.Characteristics,

                PointerToRelocations = section.PointerToRelocations,
                PointerToLineNumbers = section.PointerToLineNumbers,
                NumberOfRelocations = section.NumberOfRelocations,
                NumberOfLineNumbers = section.NumberOfLineNumbers
            };
            _sections.Add(peSection);
        }
    }
}