﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// Equivalent of <see cref="ElfBinarySection"/> but used for shadow.
/// </summary>
public sealed class ElfBinaryShadowSection : ElfShadowSection
{
    public ElfBinaryShadowSection()
    {
    }

    public Stream? Stream { get; set; }

    public override void Read(ElfReader reader)
    {
        Stream = reader.ReadAsStream(Size);
    }

    public override void Write(ElfWriter writer)
    {
        if (Stream == null) return;
        writer.Write(Stream);
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = Stream != null ? (ulong)Stream.Length : 0;
    }
}