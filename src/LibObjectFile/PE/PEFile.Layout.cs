// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Numerics;
using System.Reflection.PortableExecutable;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// A Portable Executable file that can be read, modified and written.
/// </summary>
partial class PEFile
{
    /// <summary>
    /// Updates the layout of this PE file.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to output errors.</param>
    public void UpdateLayout(DiagnosticBag diagnostics)
    {
        var context = new PELayoutContext(this, diagnostics);
        UpdateLayout(context);
    }

    private bool TryVerifyAlignment(DiagnosticBag diagnostics)
    {
        if (!BitOperations.IsPow2(OptionalHeader.FileAlignment) || OptionalHeader.FileAlignment == 0)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_FileAlignmentNotPowerOfTwo, $"File alignment {OptionalHeader.FileAlignment} is not a power of two");
            return false;
        }

        if (!BitOperations.IsPow2(OptionalHeader.SectionAlignment) || OptionalHeader.SectionAlignment == 0)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_SectionAlignmentNotPowerOfTwo, $"Section alignment {OptionalHeader.SectionAlignment} is not a power of two");
            return false;
        }

        // Ensure that SectionAlignment is greater or equal to FileAlignment
        if (OptionalHeader.SectionAlignment < OptionalHeader.FileAlignment)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_SectionAlignmentLessThanFileAlignment, $"Section alignment {OptionalHeader.SectionAlignment} is less than file alignment {OptionalHeader.FileAlignment}");
            return false;
        }

        return true;
    }
    
    /// <inheritdoc />
    protected override unsafe void UpdateLayoutCore(PELayoutContext context)
    {
        if (!TryVerifyAlignment(context.Diagnostics))
        {
            return;
        }
        
        // Update the content of Directories
        UpdateDirectories(context.Diagnostics);
        
        var position = 0U;

        // Update DOS header
        position += (uint)sizeof(PEDosHeader);
        position += (uint)_dosStub.Length;
        position += (uint)(_dosStubExtra?.Length ?? 0U);

        // Update optional header
        position = AlignHelper.AlignUp(position, 8); // PE header is aligned on 8 bytes

        // Update offset to PE header
        DosHeader._FileAddressPEHeader = position;

        position += sizeof(PESignature); // PE00 header

        // COFF header
        position += (uint)sizeof(PECoffHeader);

        // TODO: update other DosHeader fields

        var startPositionHeader = position;

        position += (uint)(IsPE32 ? RawImageOptionalHeader32.MinimumSize : RawImageOptionalHeader64.MinimumSize);

        // Update directories
        position += (uint)(Directories.Count * sizeof(RawImageDataDirectory));
        // Update the internal header
        OptionalHeader.OptionalHeaderCommonPart3.NumberOfRvaAndSizes = (uint)Directories.Count;

        CoffHeader._SizeOfOptionalHeader = (ushort)(position - startPositionHeader);

        position += (uint)(sizeof(RawImageSectionHeader) * _sections.Count);
        
        // TODO: Additional optional header size?

        // Data before sections
        foreach (var extraData in ExtraDataBeforeSections)
        {
            extraData.Position = position;
            extraData.UpdateLayout(context);
            var dataSize = (uint)extraData.Size;
            position += dataSize;
        }

        if (_sections.Count > 96)
        {
            context.Diagnostics.Error(DiagnosticId.PE_ERR_TooManySections, $"Too many sections {_sections.Count} (max 96)");
        }
        
        // Update COFF header
        CoffHeader._NumberOfSections = (ushort)_sections.Count;
        CoffHeader._PointerToSymbolTable = 0;
        CoffHeader._NumberOfSymbols = 0;

        OptionalHeader.OptionalHeaderCommonPart1.SizeOfCode = 0;
        OptionalHeader.OptionalHeaderCommonPart1.SizeOfInitializedData = 0;
        OptionalHeader.OptionalHeaderCommonPart1.SizeOfUninitializedData = 0;

        // Ensure that SectionAlignment is a multiple of FileAlignment
        position = AlignHelper.AlignUp(position, OptionalHeader.FileAlignment);
        OptionalHeader.OptionalHeaderCommonPart2.SizeOfHeaders = position;

        // Update sections
        RVA previousEndOfRVA = 0U;
        foreach (var section in _sections)
        {
            section.Position = position;
            section.UpdateLayout(context);
            if (section.Size == 0)
            {
                section.Position = 0;
            }

            if (section.RVA < previousEndOfRVA)
            {
                context.Diagnostics.Error(DiagnosticId.PE_ERR_SectionRVALessThanPrevious, $"Section {section.Name} RVA {section.RVA} is less than the previous section end RVA {previousEndOfRVA}");
            }

            var sectionSize = (uint)section.Size;
            position += sectionSize;

            var minDataSize = AlignHelper.AlignUp((uint)section.VirtualSize, OptionalHeader.FileAlignment);
            //minDataSize = section.Size > minDataSize ? (uint)section.Size : minDataSize;
            //var minDataSize = (uint)section.Size;

            if ((section.Characteristics & SectionCharacteristics.ContainsCode) != 0)
            {
                OptionalHeader.OptionalHeaderCommonPart1.SizeOfCode += minDataSize;
            }
            else if ((section.Characteristics & SectionCharacteristics.ContainsInitializedData) != 0)
            {
                OptionalHeader.OptionalHeaderCommonPart1.SizeOfInitializedData += minDataSize;
            }
            else if ((section.Characteristics & SectionCharacteristics.ContainsUninitializedData) != 0)
            {
                OptionalHeader.OptionalHeaderCommonPart1.SizeOfUninitializedData += minDataSize;
            }

            // Update the end of the RVA
            previousEndOfRVA = section.RVA + AlignHelper.AlignUp(section.VirtualSize, OptionalHeader.SectionAlignment);
        }

        // Used by tests to force the size of initialized data that seems to be calculated differently from the standard way by some linkers
        if (ForceSizeOfInitializedData != 0)
        {
            OptionalHeader.OptionalHeaderCommonPart1.SizeOfInitializedData = ForceSizeOfInitializedData;
        }

        // Update the (virtual) size of the image
        OptionalHeader.OptionalHeaderCommonPart2.SizeOfImage = previousEndOfRVA;

        // Data after sections
        foreach (var extraData in ExtraDataAfterSections)
        {
            extraData.Position = position;
            extraData.UpdateLayout(context);
            var dataSize = (uint)extraData.Size;
            position += dataSize;
        }

        // Update the size of the file
        Size = position;
    }
}
