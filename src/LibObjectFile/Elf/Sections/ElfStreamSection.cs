// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// A custom section associated with its stream of data to read/write.
/// </summary>
public sealed class ElfStreamSection : ElfSection
{
    private Stream _stream;

    public ElfStreamSection(ElfSectionType sectionType) : this(sectionType, new MemoryStream())
    {
    }

    public ElfStreamSection(ElfSectionType sectionType, Stream stream) : base(sectionType)
    {
        if (sectionType == ElfSectionType.NoBits)
        {
            throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfNoBitsSection)}` instead");
        }
        
        // Don't allow relocation or symbol table to enforce proper usage
        if (sectionType == ElfSectionType.Relocation || sectionType == ElfSectionType.RelocationAddends)
        {
            throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfRelocationTable)}` instead");
        }

        if (sectionType == ElfSectionType.SymbolTable || sectionType == ElfSectionType.DynamicLinkerSymbolTable)
        {
            throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfSymbolTable)}` instead");
        }

        _stream = stream;
        Size = (ulong)_stream.Length;
    }

    public override ulong TableEntrySize => base.TableEntrySize;

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

    public override void Write(ElfWriter writer) => writer.Write(Stream);

    protected override void UpdateLayoutCore(ElfVisitorContext context) => Size = (ulong)Stream.Length;
}