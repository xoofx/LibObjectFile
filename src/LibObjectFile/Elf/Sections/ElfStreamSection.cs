// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// A custom section associated with its stream of data to read/write.
/// </summary>
public sealed class ElfStreamSection : ElfSection
{
    private Stream _stream;

    public ElfStreamSection(ElfSectionSpecialType specialSectionType) : this(specialSectionType, new MemoryStream())
    {
    }

    public ElfStreamSection(ElfSectionSpecialType specialSectionType, Stream stream) : this(specialSectionType.GetSectionType(), stream)
    {
        Name = specialSectionType.GetDefaultName();
        Flags = specialSectionType.GetSectionFlags();
    }

    public ElfStreamSection(ElfSectionType sectionType) : this(sectionType, new MemoryStream())
    {
    }

    public ElfStreamSection(ElfSectionType sectionType, Stream stream) : base(sectionType)
    {
        _stream = stream;
        Size = (ulong)_stream.Length;
    }

    /// <summary>
    /// Gets or sets the associated stream to this section.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream = value;
            Size = (ulong)_stream.Length;
        }
    }

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        Stream = reader.ReadAsStream(Size);
    }

    public override void Write(ElfWriter writer)
    {
        Stream.Position = 0;
        writer.Write(Stream);
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context) => Size = (ulong)Stream.Length;
}