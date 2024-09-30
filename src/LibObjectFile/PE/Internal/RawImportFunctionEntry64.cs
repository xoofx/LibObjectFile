// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649
internal readonly struct RawImportFunctionEntry64
{
    private readonly ulong _hintNameTableRVA;

    public RawImportFunctionEntry64(ulong hintNameTableRVA)
    {
        _hintNameTableRVA = hintNameTableRVA;
    }

    public ulong HintNameTableRVA => IsImportByOrdinal ? 0U : _hintNameTableRVA;

    public bool IsNull => _hintNameTableRVA == 0;

    public ushort Ordinal => IsImportByOrdinal ? (ushort)_hintNameTableRVA : (ushort)0;

    public bool IsImportByOrdinal => (_hintNameTableRVA & 0x8000_0000_0000_0000UL) != 0;
}