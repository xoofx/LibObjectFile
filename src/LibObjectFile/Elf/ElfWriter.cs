// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// Base class for writing an <see cref="ElfFile"/> to a <see cref="Stream"/>.
/// </summary>
public abstract class ElfWriter : ObjectFileReaderWriter, IElfEncoder
{
    private protected ElfWriter(ElfFile file, Stream stream) : base(file, stream)
    {
    }

    public new ElfFile File => (ElfFile)base.File;

    public override bool KeepOriginalStreamForSubStreams => false;

    internal static ElfWriter Create(ElfFile file, Stream stream)
    {
        var thisComputerEncoding = BitConverter.IsLittleEndian ? ElfEncoding.Lsb : ElfEncoding.Msb;
        return file.Encoding == thisComputerEncoding ? (ElfWriter) new ElfWriterDirect(file, stream) : new ElfWriterSwap(file, stream);
    }

    public abstract void Encode(out ElfNative.Elf32_Half dest, ushort value);
    public abstract void Encode(out ElfNative.Elf64_Half dest, ushort value);
    public abstract void Encode(out ElfNative.Elf32_Word dest, uint value);
    public abstract void Encode(out ElfNative.Elf64_Word dest, uint value);
    public abstract void Encode(out ElfNative.Elf32_Sword dest, int value);
    public abstract void Encode(out ElfNative.Elf64_Sword dest, int value);
    public abstract void Encode(out ElfNative.Elf32_Xword dest, ulong value);
    public abstract void Encode(out ElfNative.Elf32_Sxword dest, long value);
    public abstract void Encode(out ElfNative.Elf64_Xword dest, ulong value);
    public abstract void Encode(out ElfNative.Elf64_Sxword dest, long value);
    public abstract void Encode(out ElfNative.Elf32_Addr dest, uint value);
    public abstract void Encode(out ElfNative.Elf64_Addr dest, ulong value);
    public abstract void Encode(out ElfNative.Elf32_Off dest, uint offset);
    public abstract void Encode(out ElfNative.Elf64_Off dest, ulong offset);
    public abstract void Encode(out ElfNative.Elf32_Section dest, ushort index);
    public abstract void Encode(out ElfNative.Elf64_Section dest, ushort index);
    public abstract void Encode(out ElfNative.Elf32_Versym dest, ushort value);
    public abstract void Encode(out ElfNative.Elf64_Versym dest, ushort value);
}