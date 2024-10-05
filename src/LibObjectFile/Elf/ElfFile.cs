// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf;

using static ElfNative;

/// <summary>
/// Defines an ELF object file that can be manipulated in memory.
/// </summary>
public sealed partial class ElfFile : ElfObject
{
    private readonly ObjectList<ElfContent> _content;
    private readonly List<ElfSection> _sections;
    private ElfSectionHeaderStringTable? _sectionHeaderStringTable;
    private readonly List<ElfSegment> _segments;

    public const int IdentSizeInBytes = ElfNative.EI_NIDENT;

    /// <summary>
    /// Creates a new instance with the default sections (null and a shadow program header table).
    /// </summary>
    public ElfFile(ElfArch arch) : this(arch, ElfFileClass.None, ElfEncoding.None)
    {
    }

    /// <summary>
    /// Creates a new instance with the default sections (null and a shadow program header table).
    /// </summary>
    public ElfFile(ElfArch arch, ElfFileClass fileClass, ElfEncoding encoding) : this(true)
    {
        Arch = arch;
        switch (arch)
        {
            case ElfArch.I386:
                FileClass = ElfFileClass.Is32;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Lsb;
                break;
            case ElfArch.X86_64:
                FileClass = ElfFileClass.Is64;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Lsb;
                break;
            case ElfArch.ARM:
                FileClass = ElfFileClass.Is32;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Lsb;
                break;
            case ElfArch.AARCH64:
                FileClass = ElfFileClass.Is64;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Lsb;
                break;
            case ElfArch.PPC:
                FileClass = ElfFileClass.Is32;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Msb;
                break;
            case ElfArch.PPC64:
                FileClass = ElfFileClass.Is64;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Msb;
                break;
            case ElfArch.MIPS:
                FileClass = ElfFileClass.Is32;
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Msb;
                break;
            default:
                Encoding = encoding != ElfEncoding.None ? encoding : ElfEncoding.Lsb;
                break;
        }
        Version = ElfNative.EV_CURRENT;
        FileType = ElfFileType.Relocatable;
    }

    internal ElfFile(bool addDefaultSections)
    {
        _content = new ObjectList<ElfContent>(this,
            ContentAdding,
            ContentAdded,
            ContentRemoving,
            ContentRemoved,
            ContentUpdating,
            ContentUpdated
        );

        AdditionalHeaderData = [];
        _sections = new List<ElfSection>();
        _segments = new List<ElfSegment>();
        Layout = new ElfFileLayout();

        if (addDefaultSections)
        {
            _content.Add(new ElfNullSection());
            _content.Add(new ElfProgramHeaderTable());
        }
    }

    /// <summary>
    /// Gets or sets the file class (i.e. 32 or 64 bits)
    /// </summary>
    public ElfFileClass FileClass { get; internal set; }

    /// <summary>
    /// Gets or sets the file encoding (i.e. LSB or MSB)
    /// </summary>
    public ElfEncoding Encoding { get; set; }

    /// <summary>
    /// Gets or sets the version of this file.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Gets or sets the OS ABI.
    /// </summary>
    public ElfOSABIEx OSABI { get; set; }

    /// <summary>
    /// Gets or sets the OS ABI version.
    /// </summary>
    public byte AbiVersion { get; set; }

    /// <summary>
    /// Gets or sets the file type (e.g executable, relocatable...)
    /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_type"/> or <see cref="ElfNative.Elf64_Ehdr.e_type"/>.
    /// </summary>
    public ElfFileType FileType { get; set; }

    /// <summary>
    /// Gets or sets the file flags (not used).
    /// </summary>
    public ElfHeaderFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets the machine architecture (e.g 386, X86_64...)
    /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_machine"/> or <see cref="ElfNative.Elf64_Ehdr.e_machine"/>.
    /// </summary>
    public ElfArchEx Arch { get; set; }

    /// <summary>
    /// Gets or sets the additional header data.
    /// </summary>
    public byte[] AdditionalHeaderData { get; set; }

    /// <summary>
    /// Entry point virtual address.
    /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_entry"/> or <see cref="ElfNative.Elf64_Ehdr.e_entry"/>.
    /// </summary>
    public ulong EntryPointAddress { get; set; }

    /// <summary>
    /// List of the segments - program headers defined by this instance.
    /// </summary>
    public ReadOnlyList<ElfSegment> Segments => _segments;
    
    /// <summary>
    /// Gets the content list defined by this instance. A content can be <see cref="ElfSection"/> or <see cref="ElfContentData"/>.
    /// </summary>
    public ObjectList<ElfContent> Content => _content;

    /// <summary>
    /// List of the sections - program headers defined by this instance.
    /// </summary>
    public ReadOnlyList<ElfSection> Sections => _sections;

    /// <summary>
    /// Gets or sets the section header string table used to store the names of the sections.
    /// Must have been added to <see cref="Sections"/>.
    /// </summary>
    public ElfSectionHeaderStringTable? SectionHeaderStringTable
    {
        get => _sectionHeaderStringTable;
        set
        {
            if (value != null)
            {
                if (value.Parent == null)
                {
                    throw new InvalidOperationException($"The {nameof(ElfSectionHeaderStringTable)} must have been added via `this.{nameof(AddSection)}(section)` before setting {nameof(SectionHeaderStringTable)}");
                }

                if (value.Parent != this)
                {
                    throw new InvalidOperationException($"This {nameof(ElfSectionHeaderStringTable)} belongs already to another {nameof(ElfFile)}. It must be removed from the other instance before adding it to this instance.");
                }
            }
            _sectionHeaderStringTable = value;
        }
    }

    /// <summary>
    /// Gets the current calculated layout of this instance (e.g offset of the program header table)
    /// </summary>
    public ElfFileLayout Layout { get; }
    
    public DiagnosticBag Verify()
    {
        var diagnostics = new DiagnosticBag();
        Verify(diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Verifies the integrity of this ELF object file.
    /// </summary>
    /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
    public void Verify(DiagnosticBag diagnostics)
    {
        var context = new ElfVisitorContext(this, diagnostics);
        Verify(context);
    }

    public override void Verify(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        if (FileClass == ElfFileClass.None)
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderFileClassNone, $"Cannot compute the layout with an {nameof(ElfFile)} having a {nameof(FileClass)} == {ElfFileClass.None}");
        }

        if (_sections.Count >= ElfNative.SHN_LORESERVE &&
            Sections[0] is not ElfNullSection)
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_MissingNullSection, $"Section count is higher than SHN_LORESERVE ({ElfNative.SHN_LORESERVE}) but the first section is not a NULL section");                
        }

        foreach (var segment in _segments)
        {
            segment.Verify(context);
        }

        // Verify all sections before doing anything else
        foreach (var section in _sections)
        {
            section.Verify(context);
        }
    }

    /// <summary>
    /// Tries to update and calculate the layout of the sections, segments and <see cref="Layout"/>.
    /// </summary>
    /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
    /// <returns><c>true</c> if the calculation of the layout is successful. otherwise <c>false</c></returns>
    public unsafe void UpdateLayout(DiagnosticBag diagnostics)
    {
        if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

        Size = 0;

        var context = new ElfVisitorContext(this, diagnostics);
            
        ulong offset = FileClass == ElfFileClass.Is32 ? (uint)sizeof(ElfNative.Elf32_Ehdr) : (uint)sizeof(ElfNative.Elf64_Ehdr);
        Layout.SizeOfElfHeader = (ushort)offset;
        Layout.OffsetOfProgramHeaderTable = 0;
        Layout.OffsetOfSectionHeaderTable = 0;
        Layout.SizeOfProgramHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Phdr) : (ushort)sizeof(ElfNative.Elf64_Phdr);
        Layout.SizeOfSectionHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Shdr) : (ushort)sizeof(ElfNative.Elf64_Shdr);
        Layout.TotalSize = offset;

        bool programHeaderTableFoundAndUpdated = false;

        // If we have any sections, prepare their offsets
        var content = CollectionsMarshal.AsSpan(_content.UnsafeList);

        // Calculate offsets of all sections in the stream
        for (var i = 0; i < content.Length; i++)
        {
            var section = content[i];
            if (i == 0 && section.Type == ElfSectionType.Null)
            {
                continue;
            }

            var align = section.Alignment == 0 ? 1 : section.Alignment;
            offset = AlignHelper.AlignUp(offset, align);
            section.Position = offset;

            if (section is ElfProgramHeaderTable programHeaderTable)
            {
                if (Segments.Count > 0)
                {
                    Layout.OffsetOfProgramHeaderTable = section.Position;
                    Layout.SizeOfProgramHeaderEntry = (ushort) section.TableEntrySize;
                    programHeaderTableFoundAndUpdated = true;
                }
            }

            if (section == SectionHeaderStringTable)
            {
                var shstrTable = SectionHeaderStringTable;
                shstrTable.Reset();

                // Prepare all section names (to calculate the name indices and the size of the SectionNames)
                // Do it in two passes to generate optimal string table
                for (var pass = 0; pass < 2; pass++)
                {
                    for (var j = 0; j < content.Count; j++)
                    {
                        var otherSection = content[j];
                        if ((j == 0 && otherSection.Type == ElfSectionType.Null)) continue;
                        if (otherSection.IsShadow) continue;
                        if (pass == 0)
                        {
                            shstrTable.ReserveString(otherSection.Name);
                        }
                        else
                        {
                            otherSection.Name = otherSection.Name.WithIndex(shstrTable.Resolve(otherSection.Name));
                        }
                    }
                }
            }

            section.UpdateLayout(context);

            // Console.WriteLine($"{section.ToString(),-50} Offset: {section.Offset:x4} Size: {section.Size:x4}");

            // A section without content doesn't count with its size
            if (!section.HasContent)
            {
                continue;
            }

            offset += section.Size;
        }

        // The Section Header Table will be put just after all the sections
        Layout.OffsetOfSectionHeaderTable = AlignHelper.AlignUp(offset, FileClass == ElfFileClass.Is32 ? 4u : 8u);

        Layout.TotalSize = Layout.OffsetOfSectionHeaderTable + (ulong)VisibleSectionCount * Layout.SizeOfSectionHeaderEntry;

        // Update program headers with offsets from auto layout
        if (Segments.Count > 0)
        {
            // Write program headers
            if (!programHeaderTableFoundAndUpdated)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_MissingProgramHeaderTableSection, $"Missing {nameof(ElfProgramHeaderTable)} shadow section for writing program headers / segments from this object file");
            }

            for (int i = 0; i < Segments.Count; i++)
            {
                var programHeader = Segments[i];
                programHeader.UpdateLayout(context);
            }
        }

        Size = offset + (ulong)VisibleSectionCount * Layout.SizeOfSectionHeaderEntry;
    }

    /// <summary>
    /// Adds a segment to <see cref="Segments"/>.
    /// </summary>
    /// <param name="segment">A segment</param>
    public void AddSegment(ElfSegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        if (segment.Parent != null)
        {
            if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
            if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfFile)} instance");
        }

        segment.Parent = this;
        segment.Index = _segments.Count;
        _segments.Add(segment);
    }

    /// <summary>
    /// Inserts a segment into <see cref="Segments"/> at the specified index.
    /// </summary>
    /// <param name="index">Index into <see cref="Segments"/> to insert the specified segment</param>
    /// <param name="segment">The segment to insert</param>
    public void InsertSegmentAt(int index, ElfSegment segment)
    {
        if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        if (segment.Parent != null)
        {
            if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
            if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfFile)} instance");
        }

        segment.Index = index;
        _segments.Insert(index, segment);
        segment.Parent = this;

        // Update the index of following segments
        for(int i = index + 1; i < _segments.Count; i++)
        {
            var nextSegment = _segments[i];
            nextSegment.Index++;
        }
    }

    /// <summary>
    /// Removes a segment from <see cref="Segments"/>
    /// </summary>
    /// <param name="segment">The segment to remove</param>
    public void RemoveSegment(ElfSegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        if (segment.Parent != this)
        {
            throw new InvalidOperationException($"Cannot remove this segment as it is not part of this {nameof(ElfFile)} instance");
        }

        var i = (int)segment.Index;
        _segments.RemoveAt(i);
        segment.ResetIndex();

        // Update indices for other sections
        for (int j = i + 1; j < _segments.Count; j++)
        {
            var nextSegments = _segments[j];
            nextSegments.Index--;
        }

        segment.Parent = null;
    }

    /// <summary>
    /// Removes a segment from <see cref="Segments"/> at the specified index.
    /// </summary>
    /// <param name="index">Index into <see cref="Segments"/> to remove the specified segment</param>
    public ElfSegment RemoveSegmentAt(int index)
    {
        if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
        var segment = _segments[index];
        RemoveSegment(segment);
        return segment;
    }

    /// <summary>
    /// Adds a section to <see cref="Sections"/>.
    /// </summary>
    /// <param name="section">A section</param>
    public TSection AddSection<TSection>(TSection section) where TSection : ElfSection
    {
        ArgumentNullException.ThrowIfNull(section);
        if (section.Parent != null)
        {
            if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
            if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfFile)} instance");
        }

        section.Parent = this;
        section.Index = _sections.Count;
        _sections.Add(section);

        if (section.IsShadow)
        {
            section.SectionIndex = 0;
            ShadowSectionCount++;
        }
        else
        {
            section.SectionIndex = VisibleSectionCount;
            VisibleSectionCount++;
        }

        // Setup the ElfSectionHeaderStringTable if not already set
        if (section is ElfSectionHeaderStringTable sectionHeaderStringTable && SectionHeaderStringTable == null)
        {
            SectionHeaderStringTable = sectionHeaderStringTable;
        }

        return section;
    }

    /// <summary>
    /// Removes a section from <see cref="Sections"/>
    /// </summary>
    /// <param name="section">The section to remove</param>
    public void RemoveSection(ElfSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (section.Parent != this)
        {
            throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfFile)} instance");
        }
        _content.Remove(section);
    }

    /// <summary>
    /// Removes a section from <see cref="Sections"/> at the specified index.
    /// </summary>
    /// <param name="index">Index into <see cref="Sections"/> to remove the specified section</param>
    public ElfSection RemoveSectionAt(int index)
    {
        if (index < 0 || index > _sections.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_sections.Count}");
        var section = _sections[index];
        RemoveSection(section);
        return section;
    }

    /// <summary>
    /// Writes this ELF object file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void Write(Stream stream)
    {
        if (!TryWrite(stream, out var diagnostics))
        {
            throw new ObjectFileException($"Invalid {nameof(ElfFile)}", diagnostics);
        }
    }

    /// <summary>
    /// Tries to write this ELF object file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="diagnostics">The output diagnostics</param>
    /// <returns><c>true</c> if writing was successful. otherwise <c>false</c></returns>
    public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var elfWriter = ElfWriter.Create(this, stream);
        diagnostics = elfWriter.Diagnostics;

        Verify(diagnostics);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        UpdateLayout(diagnostics);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        elfWriter.Write();

        return !diagnostics.HasErrors;
    }

    /// <summary>
    /// Checks if a stream contains an ELF file by checking the magic signature.
    /// </summary>
    /// <param name="stream">The stream containing potentially an ELF file</param>
    /// <returns><c>true</c> if the stream contains an ELF file. otherwise returns <c>false</c></returns>
    public static bool IsElf(Stream stream)
    {
        return IsElf(stream, out _);
    }

    /// <summary>
    /// Checks if a stream contains an ELF file by checking the magic signature.
    /// </summary>
    /// <param name="stream">The stream containing potentially an ELF file</param>
    /// <param name="encoding">Output the encoding if ELF is <c>true</c>.</param>
    /// <returns><c>true</c> if the stream contains an ELF file. otherwise returns <c>false</c></returns>
    public static bool IsElf(Stream stream, out ElfEncoding encoding)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var ident = ArrayPool<byte>.Shared.Rent(EI_NIDENT);
        encoding = ElfEncoding.None;
        try
        {
            var startPosition = stream.Position;
            var length = stream.Read(ident, 0, EI_NIDENT);
            stream.Position = startPosition;

            if (length == EI_NIDENT && (ident[EI_MAG0] == ELFMAG0 && ident[EI_MAG1] == ELFMAG1 && ident[EI_MAG2] == ELFMAG2 && ident[EI_MAG3] == ELFMAG3))
            {
                encoding = (ElfEncoding)ident[EI_DATA];
                return true;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(ident);
        }

        return false;
    }

    private static bool TryReadElfObjectFileHeader(Stream stream, [NotNullWhen(true)] out ElfFile? file)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var ident = ArrayPool<byte>.Shared.Rent(EI_NIDENT);
        file = null;
        try
        {
            var startPosition = stream.Position;
            var length = stream.Read(ident, 0, EI_NIDENT);
            stream.Position = startPosition;

            if (length == EI_NIDENT && (ident[EI_MAG0] == ELFMAG0 && ident[EI_MAG1] == ELFMAG1 && ident[EI_MAG2] == ELFMAG2 && ident[EI_MAG3] == ELFMAG3))
            {
                file =new ElfFile(false);
                file.CopyIndentFrom(ident);
                return true;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(ident);
        }

        return false;
    }

    /// <summary>
    /// Reads an <see cref="ElfFile"/> from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read ELF object file from</param>
    /// <param name="options">The options for the reader</param>
    /// <returns>An instance of <see cref="ElfFile"/> if the read was successful.</returns>
    public static ElfFile Read(Stream stream, ElfReaderOptions? options = null)
    {
        if (!TryRead(stream, out var objectFile, out var diagnostics, options))
        {
            throw new ObjectFileException($"Unexpected error while reading ELF object file", diagnostics);
        }
        return objectFile;
    }

    /// <summary>
    /// Tries to read an <see cref="ElfFile"/> from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read ELF object file from</param>
    /// <param name="objectFile"> instance of <see cref="ElfFile"/> if the read was successful.</param>
    /// <param name="diagnostics">A <see cref="DiagnosticBag"/> instance</param>
    /// <param name="options">The options for the reader</param>
    /// <returns><c>true</c> An instance of <see cref="ElfFile"/> if the read was successful.</returns>
    public static bool TryRead(Stream stream, [NotNullWhen(true)] out ElfFile? objectFile, [NotNullWhen(false)] out DiagnosticBag? diagnostics, ElfReaderOptions? options = null)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        if (!TryReadElfObjectFileHeader(stream, out objectFile))
        {
            diagnostics = new DiagnosticBag();
            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderMagic, "ELF magic header not found");
            return false;
        }

        options ??= new ElfReaderOptions();
        var reader = ElfReader.Create(objectFile, stream, options);
        diagnostics = reader.Diagnostics;

        reader.Read();

        return !reader.Diagnostics.HasErrors;
    }
    
    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
    }

    private void ContentAdding(ObjectElement parent, int index, ElfContent arg3)
    {
    }

    private void ContentAdded(ObjectElement parent, ElfContent item)
    {
        if (item is ElfSection section)
        {
            section.SectionIndex = _sections.Count;
            _sections.Add(section);
        }
    }
    
    private void ContentRemoving(ObjectElement parent, ElfContent item)
    {
    }

    private void ContentRemoved(ObjectElement parent, int index, ElfContent item)
    {
        if (item is ElfSection section)
        {
            var sectionIndex = section.SectionIndex;
            _sections.RemoveAt(sectionIndex);
            section.SectionIndex = -1;
            var sections = CollectionsMarshal.AsSpan(_sections);
            for (int i = sectionIndex; i < sections.Length; i++)
            {
                sections[i].SectionIndex = i;
            }
        }
    }
    
    private void ContentUpdating(ObjectElement parent, int index, ElfContent previousItem, ElfContent newItem)
    {
    }

    private void ContentUpdated(ObjectElement parent, int index, ElfContent previousItem, ElfContent newItem)
    {
        if (previousItem is ElfSection previousSection)
        {
            var previousSectionIndex = previousSection.SectionIndex;
            _sections.RemoveAt(previousSectionIndex);
            previousSection.SectionIndex = -1;
            if (newItem is ElfSection newSection)
            {
                newSection.SectionIndex = previousSectionIndex;
            }
            else
            {
                var sections = CollectionsMarshal.AsSpan(_sections);
                // Update the section index of the following sections
                for (int i = previousSectionIndex; i < sections.Length; i++)
                {
                    sections[i].SectionIndex = i - 1;
                }
            }
        }
        else if (newItem is ElfSection)
        {
            var sections = CollectionsMarshal.AsSpan(_sections);
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i].SectionIndex = i;
            }
        }
    }
}