// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Elf;
using LibObjectFile.IO;

namespace LibObjectFile.Ar;

/// <summary>
/// An ELF file entry.
/// </summary>
public sealed class ArElfFile : ArFile
{
    public ArElfFile()
    {
    }

    public ArElfFile(ElfFile elfFile)
    {
        ElfFile = elfFile;
    }
        
    /// <summary>
    /// Gets or sets the ELF object file.
    /// </summary>
    public ElfFile? ElfFile { get; set; }

    public override void Read(ArArchiveFileReader reader)
    {
        var startPosition = reader.Stream.Position;
        var endPosition = startPosition + (long) Size;
        ElfFile = ElfFile.Read(new SubStream(reader.Stream, reader.Stream.Position, (long)Size));
        reader.Stream.Position = endPosition;
    }

    public override void Write(ArArchiveFileWriter writer)
    {
        if (ElfFile != null)
        {
            ElfFile.TryWrite(writer.Stream, out var diagnostics);
            diagnostics.CopyTo(writer.Diagnostics);
        }
    }

    protected override void UpdateLayoutCore(ArVisitorContext context)
    {
        Size = 0;
            
        if (ElfFile != null)
        {
            ElfFile.UpdateLayout(context.Diagnostics);
            if (!context.HasErrors)
            {
                Size = ElfFile.Layout.TotalSize;
            }
        }
    }
}