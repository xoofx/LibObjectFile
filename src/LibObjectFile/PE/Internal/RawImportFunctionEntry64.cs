// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649
internal struct RawImportFunctionEntry64
{
    public RawImportFunctionEntry64(ulong hintNameTableRVA)
    {
        HintNameTableRVA = hintNameTableRVA;
    }

    public ulong HintNameTableRVA;

    public ushort Ordinal => IsImportByOrdinal ? (ushort)HintNameTableRVA : (ushort)0;

    public bool IsImportByOrdinal => (HintNameTableRVA & 0x8000_0000_0000_0000UL) != 0;
}