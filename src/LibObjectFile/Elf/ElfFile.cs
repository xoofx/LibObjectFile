// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
public sealed partial class ElfFile : ElfObject, IEnumerable<ElfContent>
{
    private readonly ObjectList<ElfContent> _content;
    private readonly List<ElfSection> _sections;
    private ElfSectionHeaderStringTable? _sectionHeaderStringTable;
    private readonly ObjectList<ElfSegment> _segments;

    public const int IdentSizeInBytes = ElfNative.EI_NIDENT;

    /// <summary>
    /// Creates a new instance with the default sections (null and a shadow program header table).
    /// </summary>
    public ElfFile(ElfArch arch) : this(arch, true)
    {
    }

    /// <summary>
    /// Creates a new instance with optional default sections (null and a shadow program header table).
    /// </summary>
    /// <param name="arch">The machine architecture for the ELF file.</param>
    /// <param name="addDefaultSections">If <c>true</c>, adds default sections (null and a shadow program header table).</param>
    public ElfFile(ElfArch arch, bool addDefaultSections) : this(arch, ElfFileClass.None, ElfEncoding.None, addDefaultSections)
    {
    }

    /// <summary>
    /// Creates a new instance with the default sections (null and a shadow program header table).
    /// </summary>
    /// <param name="arch">The machine architecture for the ELF file.</param>
    /// <param name="fileClass">The file class (32 or 64 bits) for the ELF file.</param>
    /// <param name="encoding">The encoding (LSB or MSB) for the ELF file.</param>
    public ElfFile(ElfArch arch, ElfFileClass fileClass, ElfEncoding encoding) : this(arch, fileClass, encoding, true)
    {
    }

    /// <summary>
    /// Creates a new instance with optional default sections (null and a shadow program header table).
    /// </summary>
    /// <param name="arch">The machine architecture for the ELF file.</param>
    /// <param name="fileClass">The file class (32 or 64 bits) for the ELF file.</param>
    /// <param name="encoding">The encoding (LSB or MSB) for the ELF file.</param>
    /// <param name="addDefaultSections">If <c>true</c>, adds default sections (null and a shadow program header table).</param>
    public ElfFile(ElfArch arch, ElfFileClass fileClass, ElfEncoding encoding, bool addDefaultSections) : this(addDefaultSections)
    {
        Arch = arch;
        switch (arch)
        {
            case ElfArch.I386:
                FileClass = ElfFileClass.Is32;
                Encoding = ElfEncoding.Lsb;
                break;
            case ElfArch.X86_64:
                FileClass = ElfFileClass.Is64;
                Encoding = ElfEncoding.Lsb;
                break;
            case ElfArch.ARM:
                FileClass = ElfFileClass.Is32;
                Encoding = ElfEncoding.Lsb;
                break;
            case ElfArch.AARCH64:
                FileClass = ElfFileClass.Is64;
                Encoding = ElfEncoding.Lsb;
                break;
            case ElfArch.PPC:
                FileClass = ElfFileClass.Is32;
                Encoding = ElfEncoding.Msb;
                break;
            case ElfArch.PPC64:
                FileClass = ElfFileClass.Is64;
                Encoding = ElfEncoding.Msb;
                break;
            case ElfArch.MIPS:
                FileClass = ElfFileClass.Is32;
                Encoding = ElfEncoding.Msb;
                break;
            default:
                if (fileClass == ElfFileClass.None)
                {
                    throw new ArgumentException($"Requiring a file class (32 or 64 bit) for unknown arch {arch}");
                }

                if (encoding == ElfEncoding.None)
                {
                    throw new ArgumentException($"Requiring an encoding (LSB or MSB) for unknown arch {arch}");
                }
                break;
        }

        if (fileClass != ElfFileClass.None)
        {
            FileClass = fileClass;
        }

        if (encoding != ElfEncoding.None)
        {
            Encoding = encoding;
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
        _segments = new ObjectList<ElfSegment>(this);
        Layout = new ElfFileLayout();

        _content.Add(new ElfHeaderContent());

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
    public ObjectList<ElfSegment> Segments => _segments;

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
    }

    /// <summary>
    /// Gets the current calculated layout of this instance (e.g offset of the program header table)
    /// </summary>
    public ElfFileLayout Layout { get; }

    /// <summary>
    /// Adds a new content to this ELF object file.
    /// </summary>
    /// <typeparam name="TContent">Type of the content to add</typeparam>
    /// <param name="content">The content to add</param>
    /// <returns>The added content</returns>
    public TContent Add<TContent>(TContent content) where TContent : ElfContent
    {
        _content.Add(content);
        return content;
    }

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
        ElfSectionHeaderTable? sectionHeaderTable = null;
        foreach (var content in _content)
        {
            content.Verify(context);
            if (content is ElfSectionHeaderTable sectionHeaderTableCandidate)
            {
                sectionHeaderTable = sectionHeaderTableCandidate;
            }

        }

        if (_sections.Count > 0)
        {
            if (sectionHeaderTable == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_MissingSectionHeaderTable, $"Missing {nameof(ElfSectionHeaderTable)} for writing sections from this object file");
            }
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

        ulong offset = 0;
        Layout.OffsetOfProgramHeaderTable = 0;
        Layout.SizeOfProgramHeaderEntry = 0;
        Layout.OffsetOfSectionHeaderTable = 0;
        Layout.SizeOfSectionHeaderEntry = 0;
        Layout.TotalSize = 0;

        bool programHeaderTableFoundAndUpdated = false;
        bool sectionHeaderTableFoundAndUpdated = false;

        // If we have any sections, prepare their offsets
        var contentList = CollectionsMarshal.AsSpan(_content.UnsafeList);

        // First path on non string table content
        for (var i = 0; i < contentList.Length; i++)
        {
            var content = contentList[i];
            if (content is ElfNullSection) continue;
            content.UpdateLayout(context);
        }

        // Calculate offsets of all sections in the stream
        for (var i = 0; i < contentList.Length; i++)
        {
            var content = contentList[i];
            if (content is ElfNullSection) continue;

            if (content is ElfNoBitsSection noBitsSection)
            {
                content.Position = contentList[i-1].Position + noBitsSection.PositionOffsetFromPreviousContent;
            }
            else
            {
                var align = content.FileAlignment == 0 ? 1 : content.FileAlignment;
                offset = AlignHelper.AlignUp(offset, align);
                content.Position = offset;
            }
            content.UpdateLayout(context);

            if (content is ElfProgramHeaderTable programHeaderTable && Segments.Count > 0)
            {
                Layout.OffsetOfProgramHeaderTable = content.Position;
                Layout.SizeOfProgramHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Phdr) : (ushort)sizeof(ElfNative.Elf64_Phdr);
                Layout.SizeOfProgramHeaderEntry += (ushort)programHeaderTable.AdditionalEntrySize;
                programHeaderTableFoundAndUpdated = true;
            }

            if (content is ElfSectionHeaderTable sectionHeaderTable && Sections.Count > 0)
            {
                Layout.OffsetOfSectionHeaderTable = content.Position;
                Layout.SizeOfSectionHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Shdr) : (ushort)sizeof(ElfNative.Elf64_Shdr);
                sectionHeaderTableFoundAndUpdated = true;
            }

            // A section without content doesn't count with its size
            if (content is ElfSection section && !section.HasContent)
            {
                continue;
            }

            offset += content.Size;
        }

        // Order sections by OrderInHeader
        if (Sections.Count > 0)
        {
            // If OrderInHeader is equal, we sort by SectionIndex to keep the list stable
            _sections.Sort((left, right) => left.OrderInSectionHeaderTable == right.OrderInSectionHeaderTable ? left.SectionIndex.CompareTo(right.SectionIndex) : left.OrderInSectionHeaderTable.CompareTo(right.OrderInSectionHeaderTable));

            // Update the section index
            for(int i = 0; i < _sections.Count; i++)
            {
                _sections[i].SectionIndex = i;
            }
        }
        
        // Update program headers with offsets from auto layout
        if (Segments.Count > 0)
        {
            // Write program headers
            if (!programHeaderTableFoundAndUpdated)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_MissingProgramHeaderTableSection, $"Missing {nameof(ElfProgramHeaderTable)} for writing program headers / segments from this object file");
            }
            
            for (int i = 0; i < Segments.Count; i++)
            {
                var programHeader = Segments[i];
                programHeader.UpdateLayout(context);
            }
        }

        // If we haven't found a proper section header table
        if (Sections.Count > 0 && !sectionHeaderTableFoundAndUpdated)
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_MissingSectionHeaderTableSection, $"Missing {nameof(ElfSectionHeaderTable)} for writing sections from this object file");
        }

        Layout.TotalSize = offset;
        Size = offset;
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

        Write(elfWriter);

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

        objectFile.Read(reader);

        return !reader.Diagnostics.HasErrors;
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
    }

    private void ContentAdding(ObjectElement parent, int index, ElfContent content)
    {
        if (content is ElfSectionHeaderStringTable && _sectionHeaderStringTable is not null)
        {
            throw new InvalidOperationException($"Cannot have more than one {nameof(ElfSectionHeaderStringTable)} in a {nameof(ElfFile)}");
        }
    }

    private void ContentAdded(ObjectElement parent, ElfContent item)
    {
        if (item is ElfSection section)
        {
            section.SectionIndex = _sections.Count;
            _sections.Add(section);

            if (item is ElfSectionHeaderStringTable sectionHeaderStringTable)
            {
                _sectionHeaderStringTable = sectionHeaderStringTable;
            }
        }
    }

    private void ContentRemoving(ObjectElement parent, ElfContent item)
    {
        if (item is ElfHeaderContent)
        {
            throw new InvalidOperationException($"Cannot remove the {nameof(ElfHeaderContent)} from a {nameof(ElfFile)}");
        }
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

            if (item is ElfSectionHeaderStringTable)
            {
                Debug.Assert(item == _sectionHeaderStringTable);
                _sectionHeaderStringTable = null;
            }
        }
    }

    private void ContentUpdating(ObjectElement parent, int index, ElfContent previousItem, ElfContent newItem)
    {
        if (previousItem is ElfHeaderContent)
        {
            throw new InvalidOperationException($"Cannot update the {nameof(ElfHeaderContent)} from a {nameof(ElfFile)}");
        }
        
        if (newItem is ElfSectionHeaderStringTable && previousItem is not ElfSectionHeaderStringTable && _sectionHeaderStringTable is not null && _sectionHeaderStringTable != newItem)
        {
            throw new InvalidOperationException($"Cannot have more than one {nameof(ElfSectionHeaderStringTable)} in a {nameof(ElfFile)}");
        }
    }

    private void ContentUpdated(ObjectElement parent, int index, ElfContent previousItem, ElfContent newItem)
    {
        if (previousItem is ElfSection previousSection)
        {
            var previousSectionIndex = previousSection.SectionIndex;
            previousSection.SectionIndex = -1;
            if (newItem is ElfSection newSection)
            {
                _sections[previousSectionIndex] = newSection;
                newSection.SectionIndex = previousSectionIndex;
            }
            else
            {
                _sections.RemoveAt(previousSectionIndex);
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

        if (_sectionHeaderStringTable == previousItem)
        {
            _sectionHeaderStringTable = null;
        }

        if (newItem is ElfSectionHeaderStringTable sectionHeaderStringTable)
        {
            _sectionHeaderStringTable = sectionHeaderStringTable;
        }
    }

    public List<ElfContent>.Enumerator GetEnumerator() => _content.GetEnumerator();

    IEnumerator<ElfContent> IEnumerable<ElfContent>.GetEnumerator() => _content.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_content).GetEnumerator();
}