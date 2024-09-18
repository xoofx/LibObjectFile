// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// A Portable Executable file that can be read, modified and written.
/// </summary>
public partial class PEFile : PEObject
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
        // TODO: Add default initialization
    }

    /// <summary>
    /// Internal constructor used to create a PEFile without initializing the DOS header.
    /// </summary>
    internal PEFile(bool unused)
    {
        _sections = new(this);
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
    /// Gets the sections.
    /// </summary>
    public ObjectList<PESection> Sections => _sections;
    
    public PESection AddSection(PESectionName name, uint virtualAddress, uint virtualSize)
    {
        var section = new PESection(name, virtualAddress, virtualSize)
        {
            Characteristics = PESection.GetDefaultSectionCharacteristics(name)
        };
        _sections.Add(section);
        return section;
    }
    
    public bool TryFindSection(RVA virtualAddress, [NotNullWhen(true)] out PESection? section)
        => TryFindSection(virtualAddress, 0, out section);

    public bool TryFindSection(RVA virtualAddress, uint virtualSize, [NotNullWhen(true)] out PESection? section)
    {
        nint low = 0;
        var sections = CollectionsMarshal.AsSpan(_sections.UnsafeList);
        nint high = sections.Length - 1;
        ref var firstSection = ref MemoryMarshal.GetReference(sections);

        while (low <= high)
        {
            nint mid = low + ((high - low) >>> 1);
            var midSection = Unsafe.Add(ref firstSection, mid);

            if (midSection.ContainsVirtual(virtualAddress, virtualSize))
            {
                section = midSection;
                return true;
            }

            if (midSection.VirtualAddress < virtualAddress)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        section = null;
        return false;
    }

    public bool TryFindSectionData(RVA virtualAddress, [NotNullWhen(true)] out PESectionData? sectionData)
    {
        sectionData = null;
        return TryFindSection(virtualAddress, out var section) && section.TryFindSectionData(virtualAddress, out sectionData);
    }
    
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
            count += section.DataParts.Count;
        }
        dataList.Capacity = count;

        foreach (var section in sections)
        {
            var va = section.VirtualAddress;
            foreach (var data in section.DataParts)
            {
                var size = (uint)data.Size;
                data.VirtualAddress = va;
                va += size;
                dataList.Add(data);
            }
        }

        return dataList;
    }

    public override void UpdateLayout(PEVisitorContext layoutContext)
    {
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
