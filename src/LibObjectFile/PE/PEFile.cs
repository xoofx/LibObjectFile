// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// A Portable Executable file that can be read, modified and written.
/// </summary>
public sealed partial class PEFile : PEObjectBase
{
    private byte[] _dosStub = [];
    private Stream? _dosStubExtra;
    private readonly ObjectList<PESection> _sections;

    /// <summary>
    /// Initializes a new instance of the <see cref="PEFile"/> class.
    /// </summary>
    public PEFile()
    {
        _sections = new(this);
        ExtraDataBeforeSections = new(this);
        ExtraDataAfterSections = new(this);
        // TODO: Add default initialization
    }

    /// <summary>
    /// Internal constructor used to create a PEFile without initializing the DOS header.
    /// </summary>
    internal PEFile(bool unused)
    {
        _sections = new(this);
        ExtraDataBeforeSections = new(this);
        ExtraDataAfterSections = new(this);
    }

    /// <summary>
    /// Gets the DOS header.
    /// </summary>
    public ImageDosHeader DosHeader;

    /// <summary>
    /// Gets or sets the DOS stub.
    /// </summary>
    public byte[] DosStub
    {
        get => _dosStub;

        set => _dosStub = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the DOS stub extra data (e.g Rich).
    /// </summary>
    public Stream? DosStubExtra
    {
        get => _dosStubExtra;

        set => _dosStubExtra = value;
    }

    /// <summary>
    /// Gets the COFF header.
    /// </summary>
    public ImageCoffHeader CoffHeader;

    /// <summary>
    /// Gets the optional header.
    /// </summary>
    public ImageOptionalHeader OptionalHeader;

    /// <summary>
    /// Gets a boolean indicating whether this instance is a PE32 image.
    /// </summary>
    public bool IsPE32 => OptionalHeader.Magic == ImageOptionalHeaderMagic.PE32;

    /// <summary>
    /// Gets a boolean indicating whether this instance is a PE32+ image.
    /// </summary>
    public bool IsPE32Plus => OptionalHeader.Magic == ImageOptionalHeaderMagic.PE32Plus;

    /// <summary>
    /// Gets the directories.
    /// </summary>
    /// <returns>
    /// Directories must be added/removed from <see cref="PESection.AddData"/> or <see cref="PESection.RemoveData"/>.
    /// </returns>
    public PEDirectoryTable Directories { get; } = new();

    /// <summary>
    /// Gets the data present before the sections in the file.
    /// </summary>
    public ObjectList<PEExtraData> ExtraDataBeforeSections { get; }

    /// <summary>
    /// Gets the sections.
    /// </summary>
    public ObjectList<PESection> Sections => _sections;
    
    /// <summary>
    /// Gets the data present after the sections in the file (e.g <see cref="PESecurityCertificateDirectory"/>)
    /// </summary>
    public ObjectList<PEExtraData> ExtraDataAfterSections { get; }

    public PESection AddSection(PESectionName name, uint virtualAddress, uint virtualSize)
    {
        var section = new PESection(name, virtualAddress, virtualSize)
        {
            Characteristics = PESection.GetDefaultSectionCharacteristics(name)
        };
        _sections.Add(section);
        return section;
    }

    public bool TryFindSection(RVA rva, [NotNullWhen(true)] out PESection? section)
    {
        var result = _sections.TryFindByRVA(rva, false, out var sectionObj);
        section = sectionObj as PESection;
        return result && section is not null;
    }

    public bool TryFindSection(RVA rva, uint size, [NotNullWhen(true)] out PESection? section)
        => _sections.TryFindByRVA(rva, size, out section);

    public bool TryFindContainerByRVA(RVA rva, [NotNullWhen(true)] out PEObject? container)
        => _sections.TryFindByRVA(rva, true, out container);
    
    public void RemoveSection(PESectionName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!TryGetSection(name, out var section))
        {
            throw new KeyNotFoundException($"Cannot find section with name `{name}`");
        }

        _sections.Remove(section);
    }
    
    public void ClearSections() => _sections.Clear();

    public PESection GetSection(PESectionName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!TryGetSection(name, out var section))
        {
            throw new KeyNotFoundException($"Cannot find section with name `{name}`");
        }

        return section;
    }

    public bool TryGetSection(PESectionName name, [NotNullWhen(true)] out PESection? section)
    {
        ArgumentNullException.ThrowIfNull(name);
        var sections = CollectionsMarshal.AsSpan(_sections.UnsafeList);
        foreach (var trySection in sections)
        {
            if (trySection.Name == name)
            {
                section = trySection;
                return true;
            }
        }

        section = null;
        return false;
    }

    public List<PESectionData> GetAllSectionData()
    {
        var dataList = new List<PESectionData>();

        // Precalculate the capacity
        int count = 0;
        var sections = _sections;
        foreach (var section in sections)
        {
            count += section.Content.Count;
        }
        dataList.Capacity = count;

        foreach (var section in sections)
        {
            foreach (var data in section.Content)
            {
                dataList.Add(data);
            }
        }

        return dataList;
    }

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="va">The virtual address to search for.</param>
    /// <param name="result">The section data that contains the virtual address, if found.</param>
    /// <param name="offset">The offset from the start of the section data.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">If the PEFile is not a PE32 image.</exception>
    public bool TryFindByVA(VA32 va, [NotNullWhen(true)] out PEObject? result, out RVO offset)
    {
        if (!IsPE32) throw new InvalidOperationException("PEFile is not a PE32 image");
        
        var rawRva = va - (uint)OptionalHeader.ImageBase;
        var rva = (RVA)(uint)rawRva;
        if (rawRva <= int.MaxValue && TryFindContainerByRVA(rva, out result))
        {
            offset = rva - result.RVA;
            return true;
        }

        result = null;
        offset = 0;
        return false;
    }

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="va">The virtual address to search for.</param>
    /// <param name="result">The section data that contains the virtual address, if found.</param>
    /// <param name="offset">The offset from the start of the section data.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">If the PEFile is not a PE64 image.</exception>
    public bool TryFindByVA(VA64 va, [NotNullWhen(true)] out PEObject? result, out RVO offset)
    {
        if (IsPE32) throw new InvalidOperationException("PEFile is not a PE64 image");

        var rawRva = va - OptionalHeader.ImageBase;
        var rva = (RVA)rawRva;
        if (rawRva <= uint.MaxValue && TryFindContainerByRVA(rva, out result))
        {
            offset = rva - result.RVA;
            return true;
        }

        result = null;
        offset = 0;
        return false;
    }

    /// <summary>
    /// Updates the layout of this PE file.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to output errors.</param>
    public void UpdateLayout(DiagnosticBag diagnostics)
    {
        var context = new PELayoutContext(this, diagnostics);
        UpdateLayout(context);
    }

    /// <inheritdoc />
    public override unsafe void UpdateLayout(PELayoutContext context)
    {
        var position = 0U;

        // Update DOS header
        position += (uint)sizeof(ImageDosHeader);
        position += (uint)_dosStub.Length;
        position += (uint)(_dosStubExtra?.Length ?? 0U);

        // Update optional header
        position = AlignHelper.AlignUp(position, 8); // PE header is aligned on 8 bytes

        // Update offset to PE header
        DosHeader.FileAddressPEHeader = position;

        position += sizeof(ImagePESignature); // PE00 header

        // COFF header
        position += (uint)sizeof(ImageCoffHeader);
        
        // TODO: update other DosHeader fields

        position += (uint)(IsPE32 ? sizeof(RawImageOptionalHeader32) : sizeof(RawImageOptionalHeader64));

        // Update directories
        position += (uint)(Directories.CalculateNumberOfEntries() * sizeof(ImageDataDirectory));

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
        CoffHeader.NumberOfSections = (ushort)_sections.Count;
        CoffHeader.PointerToSymbolTable = 0;
        CoffHeader.NumberOfSymbols = 0;

        OptionalHeader.SizeOfCode = 0;
        OptionalHeader.SizeOfInitializedData = 0;
        OptionalHeader.SizeOfUninitializedData = 0;

        if (!BitOperations.IsPow2(OptionalHeader.FileAlignment) || OptionalHeader.FileAlignment == 0)
        {
            context.Diagnostics.Error(DiagnosticId.PE_ERR_FileAlignmentNotPowerOfTwo, $"File alignment {OptionalHeader.FileAlignment} is not a power of two");
            return;
        }

        if (!BitOperations.IsPow2(OptionalHeader.SectionAlignment) || OptionalHeader.SectionAlignment == 0)
        {
            context.Diagnostics.Error(DiagnosticId.PE_ERR_SectionAlignmentNotPowerOfTwo, $"Section alignment {OptionalHeader.SectionAlignment} is not a power of two");
            return;
        }

        // Ensure that SectionAlignment is greater or equal to FileAlignment
        if (OptionalHeader.SectionAlignment < OptionalHeader.FileAlignment)
        {
            context.Diagnostics.Error(DiagnosticId.PE_ERR_SectionAlignmentLessThanFileAlignment, $"Section alignment {OptionalHeader.SectionAlignment} is less than file alignment {OptionalHeader.FileAlignment}");
            return;

        }

        // Ensure that SectionAlignment is a multiple of FileAlignment
        position = AlignHelper.AlignUp(position, OptionalHeader.FileAlignment);
        OptionalHeader.SizeOfHeaders = position;

        // Update sections
        RVA previousEndOfRVA = 0U;
        foreach (var section in _sections)
        {
            section.Position = position;
            section.UpdateLayout(context);
            if (section.RVA < previousEndOfRVA)
            {
                context.Diagnostics.Error(DiagnosticId.PE_ERR_SectionRVALessThanPrevious, $"Section {section.Name} RVA {section.RVA} is less than the previous section end RVA {previousEndOfRVA}");
            }

            var sectionSize = (uint)section.Size;
            position += sectionSize;

            var virtualSizeDiskAligned = AlignHelper.AlignUp(section.VirtualSize, OptionalHeader.FileAlignment);

            if ((section.Characteristics & SectionCharacteristics.ContainsCode) != 0)
            {
                OptionalHeader.SizeOfCode += virtualSizeDiskAligned;
            }
            else if ((section.Characteristics & SectionCharacteristics.ContainsInitializedData) != 0)
            {
                OptionalHeader.SizeOfInitializedData += virtualSizeDiskAligned;
            }
            else if ((section.Characteristics & SectionCharacteristics.ContainsUninitializedData) != 0)
            {
                OptionalHeader.SizeOfUninitializedData += virtualSizeDiskAligned;
            }

            // Update the end of the RVA
            previousEndOfRVA = section.RVA + AlignHelper.AlignUp(section.VirtualSize, OptionalHeader.SectionAlignment);
        }

        // Update the (virtual) size of the image
        OptionalHeader.SizeOfImage = previousEndOfRVA;

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

    protected override bool PrintMembers(StringBuilder builder)
    {
        if ((CoffHeader.Characteristics & Characteristics.Dll) != 0)
        {
            builder.Append("Dll ");
        }
        else if ((CoffHeader.Characteristics & Characteristics.ExecutableImage) != 0)
        {
            builder.Append("Exe ");
        }
        else
        {
            builder.Append("Unknown ");
        }

        switch (OptionalHeader.Magic)
        {
            case ImageOptionalHeaderMagic.PE32:
                builder.Append("PE32");
                break;
            case ImageOptionalHeaderMagic.PE32Plus:
                builder.Append("PE32+");
                break;
        }

        builder.Append($"Directories[{Directories.Count}], Sections[{Sections.Count}]");
        return true;
    }

}
