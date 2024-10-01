// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

[Flags]
public enum PETlsCharacteristics : uint
{
    Align1Bytes = 1048576, // 0x00100000
    Align2Bytes = 2097152, // 0x00200000
    Align4Bytes = Align2Bytes | Align1Bytes, // 0x00300000
    Align8Bytes = 4194304, // 0x00400000
    Align16Bytes = Align8Bytes | Align1Bytes, // 0x00500000
    Align32Bytes = Align8Bytes | Align2Bytes, // 0x00600000
    Align64Bytes = Align32Bytes | Align1Bytes, // 0x00700000
    Align128Bytes = 8388608, // 0x00800000
    Align256Bytes = Align128Bytes | Align1Bytes, // 0x00900000
    Align512Bytes = Align128Bytes | Align2Bytes, // 0x00A00000
    Align1024Bytes = Align512Bytes | Align1Bytes, // 0x00B00000
    Align2048Bytes = Align128Bytes | Align8Bytes, // 0x00C00000
    Align4096Bytes = Align2048Bytes | Align1Bytes, // 0x00D00000
    Align8192Bytes = Align2048Bytes | Align2Bytes, // 0x00E00000
    AlignMask = Align8192Bytes | Align1Bytes, // 0x00F00000
}