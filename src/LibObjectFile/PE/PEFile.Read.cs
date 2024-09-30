// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
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
        diagnostics.EnableStackTrace = options.EnableStackTrace;
        peFile.Read(reader);

        return !reader.Diagnostics.HasErrors;
    }

    public override unsafe void Read(PEImageReader reader)
    {
        Debug.Assert(Unsafe.SizeOf<PEDosHeader>() == 64);

        Position = 0;
        Size = reader.Length;

        var diagnostics = reader.Diagnostics;
        int read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref DosHeader, 1)));
        if (read != Unsafe.SizeOf<PEDosHeader>())
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosHeaderSize, "Invalid DOS header");
            return;
        }

        if (DosHeader.Magic != PEDosMagic.DOS)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosHeaderMagic, "Invalid DOS header");
            return;
        }

        var pePosition = DosHeader.FileAddressPEHeader;

        if (pePosition < sizeof(PEDosHeader))
        {
            if (pePosition < 4)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidPEHeaderPosition, "Invalid PE header position");
                return;
            }

            _unsafeNegativePEHeaderOffset = (int)pePosition - sizeof(PEDosHeader);
        }
        else
        {
            // Read the DOS stub
            var dosStubSize = DosHeader.SizeOfParagraphsHeader * 16;

            if (dosStubSize + sizeof(PEDosHeader) > pePosition)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosStubSize, $"Invalid DOS stub size {dosStubSize} going beyond the PE header");
                return;
            }
            
            if (dosStubSize > 0)
            {
                var dosStub = new byte[dosStubSize];
                read = reader.Read(dosStub);
                if (read != dosStubSize)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_InvalidDosStubSize, "Invalid DOS stub");
                    return;
                }

                _dosStub = dosStub;
            }
            else
            {
                _dosStub = [];
            }

            var dosHeaderAndStubSize = sizeof(PEDosHeader) + dosStubSize;

            // Read any DOS stub extra data (e.g Rich)
            if (dosHeaderAndStubSize < DosHeader.FileAddressPEHeader)
            {
                _dosStubExtra = reader.ReadAsStream((ulong)(DosHeader.FileAddressPEHeader - dosHeaderAndStubSize));
            }
        }

        // Read the PE header
        reader.Position = DosHeader.FileAddressPEHeader;

        var signature = default(PESignature);
        read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref signature, 1)));
        if (read != sizeof(PESignature))
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidPESignature, "Invalid PE signature");
            return;
        }

        if (signature != PESignature.PE)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidPESignature, $"Invalid PE signature 0x{(uint)signature:X8}");
            return;
        }

        // Read the COFF header
        Debug.Assert(Unsafe.SizeOf<PECoffHeader>() == 20);
        var coffHeader = default(PECoffHeader);
        read = reader.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref coffHeader, 1)));
        if (read != Unsafe.SizeOf<PECoffHeader>())
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidCoffHeaderSize, "Invalid COFF header");
            return;
        }
        CoffHeader = coffHeader;

        var positionAfterCoffHeader = reader.Position;

        // Cannot be smaller than the magic
        if (CoffHeader.SizeOfOptionalHeader < sizeof(PEOptionalHeaderMagic))
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidOptionalHeaderSize, $"Invalid optional header size {CoffHeader.SizeOfOptionalHeader}");
            return;
        }

        var magic = (PEOptionalHeaderMagic)reader.ReadU16();

        int minimumSizeOfOptionalHeaderToRead = 0;
        
        // Process known PE32/PE32+ headers
        if (magic == PEOptionalHeaderMagic.PE32)
        {
            minimumSizeOfOptionalHeaderToRead = RawImageOptionalHeader32.MinimumSize;
        }
        else if (magic == PEOptionalHeaderMagic.PE32Plus)
        {
            minimumSizeOfOptionalHeaderToRead = RawImageOptionalHeader64.MinimumSize;
        }
        else
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidOptionalHeaderMagic, $"Invalid optional header PE magic 0x{(uint)magic:X8}");
            return;
        }

        Span<byte> optionalHeader =  stackalloc byte[minimumSizeOfOptionalHeaderToRead];
        MemoryMarshal.Write(optionalHeader, (ushort)magic);

        // We have already read the magic number
        var minimumSpan = optionalHeader.Slice(sizeof(PEOptionalHeaderMagic));
        read = reader.Read(minimumSpan);

        // The real size read (in case of tricked Tiny PE)
        optionalHeader = optionalHeader.Slice(0, read + sizeof(PEOptionalHeaderMagic));
        
        Debug.Assert(Unsafe.SizeOf<RawImageOptionalHeader32>() == 224);
        Debug.Assert(Unsafe.SizeOf<RawImageOptionalHeader64>() == 240);

        // Process known PE32/PE32+ headers
        if (magic == PEOptionalHeaderMagic.PE32)
        {
            var optionalHeader32 = new RawImageOptionalHeader32();
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref optionalHeader32, 1));
            if (span.Length > optionalHeader.Length)
            {
                span = span.Slice(0, optionalHeader.Length);
            }

            optionalHeader.CopyTo(span);

            OptionalHeader.OptionalHeaderCommonPart1 = optionalHeader32.Common;
            OptionalHeader.OptionalHeaderBase32 = optionalHeader32.Base32;
            OptionalHeader.OptionalHeaderCommonPart2 = optionalHeader32.Common2;
            OptionalHeader.OptionalHeaderSize32 = optionalHeader32.Size32;
            OptionalHeader.OptionalHeaderCommonPart3 = optionalHeader32.Common3;
        }
        else
        {
            var optionalHeader64 = new RawImageOptionalHeader64();
            // Skip 2 bytes as we read already the magic number
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref optionalHeader64, 1));
            if (span.Length > optionalHeader.Length)
            {
                span = span.Slice(0, optionalHeader.Length);
            }

            optionalHeader.CopyTo(span);

            OptionalHeader.OptionalHeaderCommonPart1 = optionalHeader64.Common;
            OptionalHeader.OptionalHeaderBase64 = optionalHeader64.Base64;
            OptionalHeader.OptionalHeaderCommonPart2 = optionalHeader64.Common2;
            OptionalHeader.OptionalHeaderSize64 = optionalHeader64.Size64;
            OptionalHeader.OptionalHeaderCommonPart3 = optionalHeader64.Common3;
        }

        // Read Directory headers
        using var pooledSpanDirectories = TempSpan<RawImageDataDirectory>.Create(stackalloc byte[16 * sizeof(RawImageDataDirectory)], (int)OptionalHeader.NumberOfRvaAndSizes, out var rawDirectories);

        // Sets the number of entries in the data directory
        Directories.Count = (int)OptionalHeader.NumberOfRvaAndSizes;

        if (OptionalHeader.NumberOfRvaAndSizes > 0)
        {
            var span = MemoryMarshal.AsBytes(rawDirectories);
            read = reader.Read(span);
            if (read != span.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_InvalidNumberOfDataDirectories, $"Invalid number of data directory {OptionalHeader.NumberOfRvaAndSizes} at position {reader.Position}");
                return;
            }
        }
        
        // Read sections
        reader.Position = positionAfterCoffHeader + CoffHeader.SizeOfOptionalHeader;
        
        Debug.Assert(Unsafe.SizeOf<RawImageSectionHeader>() == 40);

        using var pooledSpanSections = TempSpan<RawImageSectionHeader>.Create((int)CoffHeader.NumberOfSections, out var rawSections);
        read = reader.Read(pooledSpanSections.AsBytes);

        if (read != pooledSpanSections.AsBytes.Length)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidSectionHeadersSize, "Invalid section headers");
            return;
        }

        // Read all sections and directories
        ReadSectionsAndDirectories(reader, rawSections, rawDirectories);

        // Set the size to the full size of the file
        Size = reader.Length;
    }

    private void ReadSectionsAndDirectories(PEImageReader reader, ReadOnlySpan<RawImageSectionHeader> rawSectionHeaders, ReadOnlySpan<RawImageDataDirectory> rawDirectories)
    {
        _sections.Clear();

        uint positionAfterHeaders = (uint)reader.Position;
        uint positionFirstSection = positionAfterHeaders;

        // Load any data stored before the sections
        if (rawSectionHeaders.Length > 0)
        {
            positionFirstSection = rawSectionHeaders[0].PointerToRawData;
        }

        uint positionAfterLastSection = positionFirstSection;
        
        // Create sections
        foreach (var rawSection in rawSectionHeaders)
        {
            // We don't validate the name
            var section = new PESection( new PESectionName(rawSection.NameAsString, false), rawSection.RVA)
            {
                Position = rawSection.PointerToRawData,
                Size = rawSection.SizeOfRawData,
                Characteristics = rawSection.Characteristics,
            };

            // Sets the virtual size of the section (auto or fixed)
            section.SetVirtualSizeMode(
                rawSection.VirtualSize <= rawSection.SizeOfRawData
                    ? PESectionVirtualSizeMode.Auto
                    : PESectionVirtualSizeMode.Fixed,
                rawSection.VirtualSize);

            var positionAfterSection = rawSection.PointerToRawData + rawSection.SizeOfRawData;
            if (positionAfterSection > positionAfterLastSection)
            {
                positionAfterLastSection = positionAfterSection;
            }

            _sections.Add(section);
        }

        // A list that contains all the section data that we have created (e.g. from directories, import tables...)
        var sectionDataList = new List<PESectionData>();
        var extraDataList = new List<PEExtraData>();

        // Create directories and find the section for each directory
        var maxNumberOfDirectory = (int)Math.Min(OptionalHeader.NumberOfRvaAndSizes, 15);
        for (int i = 0; i < maxNumberOfDirectory && i < rawDirectories.Length; i++)
        {
            var directoryEntry = rawDirectories[i];
            if (directoryEntry.RVA == 0 || directoryEntry.Size == 0)
            {
                continue;
            }

            var kind = (PEDataDirectoryKind)i;
            if (kind == PEDataDirectoryKind.SecurityCertificate)
            {
                var directory = new PESecurityCertificateDirectory
                {
                    // The PE certificate directory is a special case as it is not a standard directory. It doesn't use RVA but the position in the file
                    Position = (uint)directoryEntry.RVA,
                    Size = directoryEntry.Size
                };

                Directories.Set(directory);

                // The PESecurityDirectory is a special case as it doesn't use RVA but the position in the file. It belongs after the sections to the extra data
                extraDataList.Add(directory);
                
                directory.Read(reader);
            }
            else
            {

                if (!TryFindSection(directoryEntry.RVA, directoryEntry.Size, out var peSection))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidDataDirectorySection, $"Unable to find the section for the DataDirectory {(PEDataDirectoryKind)i} at virtual address {directoryEntry.RVA}, size {directoryEntry.Size}");
                    continue;
                }

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
        for (var i = 0; i < Directories.Count; i++)
        {
            var entry = Directories[i];
            if (entry is not PEDataDirectory directory)
            {
                continue;
            }

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
            if (!TryFindByPosition((uint)vObj.Position, out var container))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Unable to find a container for {vObj} at position {vObj.Position}");
                return;
            }

            ObjectList<PESectionData> dataParts;
            
            if (container is PECompositeSectionData compositeContainer)
            {
                dataParts = compositeContainer.Content;
            }
            else if (container is PESection section)
            {
                dataParts = section.Content;
            }
            else
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid container {container} for {vObj} at position {vObj.Position}");
                return;
            }
            
            int insertIndex = 0;
            for (; insertIndex < dataParts.Count; insertIndex++)
            {
                var sectionData = dataParts[insertIndex];
                if (vObj.Position < sectionData.Position)
                {
                    break;
                }
            }

            dataParts.Insert(insertIndex, vObj);
        }

        // Make sure that we have a proper stream for all directories
        for (var i = 0; i < Directories.Count; i++)
        {
            var entry = Directories[i];
            if (entry is PEDataDirectory directory)
            {
                FillDirectoryWithStreams(reader, directory);
            }
        }

        // Create all remaining extra data section data (before and after sections)
        if (positionFirstSection > positionAfterHeaders)
        {
            FillExtraDataWithMissingStreams(reader, this, ExtraDataBeforeSections, positionAfterHeaders, positionFirstSection - positionAfterHeaders);
        }

        FillExtraDataWithMissingStreams(reader, this, ExtraDataAfterSections, positionAfterLastSection, reader.Length - positionAfterLastSection);
        
        // Create Stream data sections for remaining data per section based on directories already loaded
        for (var i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            FillSectionDataWithMissingStreams(reader, section, section.Content, (uint)section.Position, (uint)section.Size, rawSectionHeaders[i].VirtualSize);

            var previousSize = rawSectionHeaders[i].SizeOfRawData;

            section.UpdateLayout(reader);

            var newSize = section.Size;
            if (newSize != previousSize)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid size for section {section} at {section.Position} with calculated size 0x{section.Size:X} != size from disk 0x{previousSize:X}");
                return;
            }
        }

        // Final binding of directories with the actual section data
        for (var i = 0; i < Directories.Count; i++)
        {
            var entry = Directories[i];

            if (entry is PEDataDirectory directory)
            {
                Debug.Assert(directory.RVA == rawDirectories[i].RVA);
                directory.Bind(reader);
            }
        }
        
        // Validate the VirtualAddress of the directories
        for (int i = 0; i < maxNumberOfDirectory && i < rawDirectories.Length; i++)
        {
            var directoryEntry = rawDirectories[i];
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
    private static void FillSectionDataWithMissingStreams(PEImageReader imageReader, PEObjectBase parent, ObjectList<PESectionData> dataParts, uint startPosition, uint totalSize, uint? totalVirtualSizeOpt)
    {
        var currentPosition = startPosition;
        var endPosition = startPosition + totalSize;

        var listOrderedByPosition = dataParts.UnsafeList;

        // Early exit if we don't have any data
        if (totalSize == 0 && listOrderedByPosition.Count == 0)
        {
            return;
        }

        // We are working on position, while the list is ordered by VirtualAddress
        listOrderedByPosition.Sort((a, b) => a.Position.CompareTo(b.Position));
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            data.Index = i;
        }
        
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];

            // Make sure we align the position to the required alignment of the next data
            //currentPosition = AlignHelper.AlignUp(currentPosition, data.GetRequiredPositionAlignment(imageReader.File));

            if (currentPosition < data.Position)
            {
                var size = (uint)(data.Position - currentPosition);
                imageReader.Position = currentPosition;

                var sectionData = new PEStreamSectionData(imageReader.ReadAsStream(size))
                {
                    Position = currentPosition,
                    Parent = parent,
                };

                listOrderedByPosition.Insert(i, sectionData);
                currentPosition += size;
                i++;
            }
            else if (currentPosition > data.Position)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid section data position 0x{currentPosition:X} > 0x{data.Position:X} in {parent}");
                return;
            }

            var dataSize = (uint)data.Size;
            //var dataSize = AlignHelper.AlignUp(data.Size, data.GetRequiredSizeAlignment(imageReader.File));
            currentPosition += dataSize;
        }

        if (currentPosition < endPosition)
        {
            var size = endPosition - currentPosition;

            uint paddingSize = 0;

            if (totalVirtualSizeOpt.HasValue && totalVirtualSizeOpt.Value < totalSize)
            {
                paddingSize = totalSize - totalVirtualSizeOpt.Value;
            }

            if (paddingSize > size)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid padding size 0x{paddingSize:X} while remaining size is 0x{size:X} at position 0x{currentPosition:X} > 0x{endPosition:X} in {parent}");
                return;
            }

            size -= paddingSize;

            // If we have actual data before the padding, create a normal stream
            if (size > 0)
            {
                imageReader.Position = currentPosition;
                var sectionData = new PEStreamSectionData(imageReader.ReadAsStream(size))
                {
                    Position = currentPosition,
                    Parent = parent,
                };
                listOrderedByPosition.Add(sectionData);
                currentPosition += size;
            }

            // Create a padding stream if we have a virtual size smaller than the actual size
            if (paddingSize > 0)
            {
                imageReader.Position = currentPosition;
                ((PESection)parent).PaddingStream = imageReader.ReadAsStream(paddingSize);
            }
        }
        else if (currentPosition > endPosition)
        {
            imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid section data position 0x{currentPosition:X} > 0x{endPosition:X} in {parent}");
        }

        // Make sure to update the indices after inserting the missing streams
        for (var i = 0; i < listOrderedByPosition.Count; i++)
        {
            var data = listOrderedByPosition[i];
            data.Index = i;
        }
    }
    
    private static void FillExtraDataWithMissingStreams(PEImageReader imageReader, PEObjectBase parent, ObjectList<PEExtraData> list, ulong startPosition, ulong totalSize)
    {
        var currentPosition = startPosition;
        var endPosition = startPosition + totalSize;

        // We are working on position, while the list is ordered by VirtualAddress
        var listOrderedByPosition = list.UnsafeList;

        // Early exit if we don't have any data
        if (totalSize == 0 && listOrderedByPosition.Count == 0)
        {
            return;
        }

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

                var extraData = new PEStreamExtraData(imageReader.ReadAsStream(size))
                {
                    Position = currentPosition,
                    Parent = parent,
                };

                listOrderedByPosition.Insert(i, extraData);
                currentPosition = data.Position;
                i++;
            }
            else if (currentPosition > data.Position)
            {
                imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid extra data position 0x{currentPosition:X} > 0x{data.Position:X}");
                return;
            }

            currentPosition += data.Size;
        }

        if (currentPosition < endPosition)
        {
            var size = endPosition - currentPosition;
            imageReader.Position = currentPosition;
            var extraData = new PEStreamExtraData(imageReader.ReadAsStream(size))
            {
                Position = currentPosition,
                Parent = parent,
            };

            listOrderedByPosition.Add(extraData);
        }
        else if (currentPosition > endPosition)
        {
            imageReader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Invalid extra data position 0x{currentPosition:X} > 0x{endPosition:X} in {parent}");
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
        FillSectionDataWithMissingStreams(imageReader, directory, directory.Content, (uint)(directory.Position + directory.HeaderSize), (uint)(directory.Size - directory.HeaderSize), null);
    }
}