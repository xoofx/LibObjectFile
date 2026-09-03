// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// What a symbol refers to, held in the <c>N_TYPE</c> bits of a symbol's type byte.
/// </summary>
public enum MachOSymbolKind : byte
{
    /// <summary>The symbol is not defined in this image (<c>N_UNDF</c>).</summary>
    Undefined = 0x0,
    /// <summary>The symbol has an absolute value that no section owns (<c>N_ABS</c>).</summary>
    Absolute = 0x2,
    /// <summary>The symbol is an alias for another, named by its string index (<c>N_INDR</c>).</summary>
    Indirect = 0xa,
    /// <summary>The symbol was undefined in a prebound image (<c>N_PBUD</c>).</summary>
    PreboundUndefined = 0xc,
    /// <summary>The symbol is defined in the section given by its section index (<c>N_SECT</c>).</summary>
    Section = 0xe,
}
