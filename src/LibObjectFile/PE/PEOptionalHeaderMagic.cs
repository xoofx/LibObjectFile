// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public enum PEOptionalHeaderMagic : ushort
{
    /// <summary>
    /// PE32
    /// </summary>
    PE32 = 0x10b,

    /// <summary>
    /// PE32+
    /// </summary>
    PE32Plus = 0x20b,
}