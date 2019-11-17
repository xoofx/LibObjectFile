// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// An encoder for the various Elf types that swap LSB/MSB ordering based on a mismatch between the current machine and file ordering.
    /// </summary>
    internal readonly struct ElfEncoderSwap : IElfEncoder
    {
        public void Encode(out ElfNative.Elf32_Half dest, ushort value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Half dest, ushort value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Word dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Word dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Sword dest, int value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Sword dest, int value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Xword dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Sxword dest, long value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Xword dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Sxword dest, long value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Addr dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Addr dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf32_Off dest, uint offset)
        {
            dest = BinaryUtil.SwapBits(offset);
        }

        public void Encode(out ElfNative.Elf64_Off dest, ulong offset)
        {
            dest = BinaryUtil.SwapBits(offset);
        }

        public void Encode(out ElfNative.Elf32_Section dest, ushort index)
        {
            dest = BinaryUtil.SwapBits(index);
        }

        public void Encode(out ElfNative.Elf64_Section dest, ushort index)
        {
            dest = BinaryUtil.SwapBits(index);
        }

        public void Encode(out ElfNative.Elf32_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf32_Half)BinaryUtil.SwapBits(value);
        }

        public void Encode(out ElfNative.Elf64_Versym dest, ushort value)
        {
            dest = (ElfNative.Elf64_Half)BinaryUtil.SwapBits(value);
        }
    }
}