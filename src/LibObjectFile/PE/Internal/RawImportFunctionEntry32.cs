// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649
internal readonly struct RawImportFunctionEntry32
{
    private readonly uint _hintNameTableRVA;

    public RawImportFunctionEntry32(uint hintNameTableRVA)
    {
        _hintNameTableRVA = hintNameTableRVA;
    }

    public uint HintNameTableRVA => IsImportByOrdinal ? 0U : _hintNameTableRVA;

    public bool IsNull => HintNameTableRVA == 0;

    public bool IsImportByOrdinal => (HintNameTableRVA & 0x8000_0000U) != 0;

    public ushort Ordinal => IsImportByOrdinal ? (ushort)HintNameTableRVA : (ushort)0;
}