// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    public struct ElfRelocation
    {
        public ulong Offset { get; set; }

        public ElfRelocationType Type { get; set; }

        public uint SymbolIndex { get; set; }

        public long Addend { get; set; }

        public uint Info32 =>
            ((uint) SymbolIndex << 8) | ((Type.Value & 0xFF));

        public ulong Info64 =>
            ((ulong)SymbolIndex << 32) | (Type.Value);
        
        public override string ToString()
        {
            return $"{nameof(Offset)}: 0x{Offset:X16}, {nameof(Type)}: {Type}, {nameof(SymbolIndex)}: {SymbolIndex}, {nameof(Addend)}: 0x{Addend:X16}";
        }
    }
}