﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf;

public enum DwarfArrayOrderingKind : byte
{
    RowMajor = DwarfNative.DW_ORD_row_major,

    ColumnMajor = DwarfNative.DW_ORD_col_major,
}