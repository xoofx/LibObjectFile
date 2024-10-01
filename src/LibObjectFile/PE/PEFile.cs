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

    static PEFile()
    {
        // Required for resolving code pages
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEFile"/> class PE32+.
    /// </summary>
    public PEFile() : this(PEOptionalHeaderMagic.PE32Plus)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEFile"/> class.
    /// </summary>
    public PEFile(PEOptionalHeaderMagic magic)
    {
        _sections = new(this);
        Directories = new();
        ExtraDataBeforeSections = new(this);
        ExtraDataAfterSections = new(this);

        DosHeader = PEDosHeader.Default;
        DosStub = PEDosHeader.DefaultDosStub.ToArray();

        CoffHeader = new()
        {
            Machine = Machine.Amd64,
            TimeDateStamp = 0,
            Characteristics = Characteristics.ExecutableImage | Characteristics.LargeAddressAware
        };

        OptionalHeader = new PEOptionalHeader(this, magic);

        // Support all directories by default
        Directories.Count = 16;

        // Update the layout which is only going to calculate the size.
        UpdateLayout(new());
    }

    /// <summary>
    /// Internal constructor used to create a PEFile without initializing the DOS header.
    /// </summary>
    internal PEFile(bool unused)
    {
        _sections = new(this);
        Directories = new();
        ExtraDataBeforeSections = new(this);
        ExtraDataAfterSections = new(this);

        OptionalHeader = new PEOptionalHeader(this);
    }

    /// <summary>
    /// Gets the DOS header.
    /// </summary>
    public PEDosHeader DosHeader;

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
    public PECoffHeader CoffHeader;

    /// <summary>
    /// Gets the optional header.
    /// </summary>
    public PEOptionalHeader OptionalHeader { get; }

    /// <summary>
    /// Gets a boolean indicating whether this instance is a PE32 image.
    /// </summary>
    public bool IsPE32 => OptionalHeader.Magic == PEOptionalHeaderMagic.PE32;

    /// <summary>
    /// Gets a boolean indicating whether this instance is a PE32+ image.
    /// </summary>
    public bool IsPE32Plus => OptionalHeader.Magic == PEOptionalHeaderMagic.PE32Plus;

    /// <summary>
    /// Gets the directories.
    /// </summary>
    /// <returns>
    /// Directories must be added/removed from <see cref="PESection.AddData"/> or <see cref="PESection.RemoveData"/>.
    /// </returns>
    public PEDirectoryTable Directories { get; }

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

    /// <summary>
    /// Used for testing when the size of initialized data is not correctly computed because an existing PE is using a different algorithm for computing the size.
    /// </summary>
    internal uint ForceSizeOfInitializedData { get; set; }

    public override bool HasChildren => true;


    public PESection AddSection(PESectionName name, RVA rva)
    {
        var section = new PESection(name, rva)
        {
            Characteristics = PESection.GetDefaultSectionCharacteristics(name)
        };
        section.SetVirtualSizeModeToAuto();
        _sections.Add(section);
        return section;
    }

    public PESection AddSection(PESectionName name, RVA rva, uint virtualSize)
    {
        var section = new PESection(name, rva)
        {
            Characteristics = PESection.GetDefaultSectionCharacteristics(name)
        };
        section.SetVirtualSizeModeToFixed(virtualSize);
        _sections.Add(section);
        return section;
    }

    public bool TryFindSectionByRVA(RVA rva, [NotNullWhen(true)] out PESection? section)
    {
        var result = _sections.TryFindByRVA(rva, false, out var sectionObj);
        section = sectionObj as PESection;
        return result && section is not null;
    }

    public bool TryFindSectionByRVA(RVA rva, uint size, [NotNullWhen(true)] out PESection? section)
        => _sections.TryFindByRVA(rva, size, out section);

    public bool TryFindByRVA(RVA rva, [NotNullWhen(true)] out PEObject? container)
        => _sections.TryFindByRVA(rva, true, out container);


    protected override bool TryFindByPositionInChildren(uint position, [NotNullWhen(true)] out PEObjectBase? result)
        => _sections.TryFindByPosition(position, true, out result) ||
               ExtraDataBeforeSections.TryFindByPosition(position, true, out result) ||
               ExtraDataAfterSections.TryFindByPosition(position, true, out result);

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
        if (rawRva <= int.MaxValue && TryFindByRVA(rva, out result))
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
        if (rawRva <= uint.MaxValue && TryFindByRVA(rva, out result))
        {
            offset = rva - result.RVA;
            return true;
        }

        result = null;
        offset = 0;
        return false;
    }

    /// <summary>
    /// Automatically update directories from the content of the sections.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to output errors.</param>
    public void UpdateDirectories(DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Directories.Clear();

        foreach (var section in _sections)
        {
            foreach (var content in section.Content)
            {
                if (content is PECompositeSectionData compositeSectionData)
                {
                    compositeSectionData.UpdateDirectories(this, diagnostics);
                }
            }
        }

        foreach (var extraData in ExtraDataAfterSections)
        {
            if (extraData is PESecurityCertificateDirectory securityCertificate)
            {
                var existingSecurityCertificate = Directories[PEDataDirectoryKind.SecurityCertificate];
                if (existingSecurityCertificate is not null)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_DirectoryWithSameKindAlreadyAdded, $"A directory with the kind {PEDataDirectoryKind.SecurityCertificate} was already found {existingSecurityCertificate} while trying to add new directory {securityCertificate}");
                }
                else
                {
                    Directories[PEDataDirectoryKind.SecurityCertificate] = securityCertificate;
                }
            }
        }
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
            case PEOptionalHeaderMagic.PE32:
                builder.Append("PE32");
                break;
            case PEOptionalHeaderMagic.PE32Plus:
                builder.Append("PE32+");
                break;
        }

        builder.Append($"Directories[{Directories.Count}], Sections[{Sections.Count}]");
        return true;
    }
}
