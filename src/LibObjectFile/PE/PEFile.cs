// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// A Portable Executable file that can be read, modified and written.
/// </summary>
public partial class PEFile : PEObject
{
    private byte[] _dosStub = [];
    private Stream? _dosStubExtra;
    private readonly List<PESection> _sections = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PEFile"/> class.
    /// </summary>
    public PEFile()
    {
        // TODO: Add default initialization
    }

    /// <summary>
    /// Internal constructor used to create a PEFile without initializing the DOS header.
    /// </summary>
    internal PEFile(bool unused)
    {
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


    public IReadOnlyList<PESection> Sections => _sections;
    
    public PESection AddSection(string name, uint virtualAddress, uint virtualSize, SectionCharacteristics characteristics = SectionCharacteristics.MemRead)
    {
        var section = new PESection(this, name)
        {
            VirtualAddress = virtualAddress,
            VirtualSize = virtualSize,
            Characteristics = characteristics
        };
        _sections.Add(section);
        return section;
    }

    public void RemoveSectionAt(int index) => _sections.RemoveAt(index);

    public void RemoveSection(PESection section) => _sections.Remove(section);
    
    public bool TryFindSection(uint virtualAddress, [NotNullWhen(true)] out PESection? section)
        => TryFindSection(virtualAddress, 0, out section);

    public bool TryFindSection(uint virtualAddress, uint virtualSize, [NotNullWhen(true)] out PESection? section)
    {
        var sections = CollectionsMarshal.AsSpan(_sections);
        foreach (var trySection in sections)
        {
            if (trySection.ContainsVirtual(virtualAddress, virtualSize))
            {
                section = trySection;
                return true;
            }
        }

        section = null;
        return false;
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
        var sections = CollectionsMarshal.AsSpan(_sections);
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
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        // TODO
    }
}