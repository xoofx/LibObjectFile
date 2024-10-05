// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf;

using static ElfNative;

/// <summary>
/// Internal implementation of <see cref="ElfWriter"/> to write to a stream an <see cref="ElfFile"/> instance.
/// </summary>
/// <typeparam name="TEncoder">The encoder used for LSB/MSB conversion</typeparam>
internal abstract class ElfWriter<TEncoder> : ElfWriter where TEncoder : struct, IElfEncoder
{
    private TEncoder _encoder;

    protected ElfWriter(ElfFile file, Stream stream) : base(file, stream)
    {
        _encoder = new TEncoder();
    }

    public override void Encode(out Elf32_Half dest, ushort value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Half dest, ushort value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Word dest, uint value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Word dest, uint value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Sword dest, int value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Sword dest, int value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Xword dest, ulong value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Sxword dest, long value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Xword dest, ulong value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Sxword dest, long value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Addr dest, uint value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Addr dest, ulong value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf32_Off dest, uint offset)
    {
        _encoder.Encode(out dest, offset);
    }

    public override void Encode(out Elf64_Off dest, ulong offset)
    {
        _encoder.Encode(out dest, offset);
    }

    public override void Encode(out Elf32_Section dest, ushort index)
    {
        _encoder.Encode(out dest, index);
    }

    public override void Encode(out Elf64_Section dest, ushort index)
    {
        _encoder.Encode(out dest, index);
    }

    public override void Encode(out Elf32_Versym dest, ushort value)
    {
        _encoder.Encode(out dest, value);
    }

    public override void Encode(out Elf64_Versym dest, ushort value)
    {
        _encoder.Encode(out dest, value);
    }
}