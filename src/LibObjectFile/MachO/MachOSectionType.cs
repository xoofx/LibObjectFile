// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Type of a section, held in the low byte of the section <c>flags</c> field
/// (<c>SECTION_TYPE</c>).
/// </summary>
/// <remarks>
/// The type decides whether the section occupies space in the file. <see cref="ZeroFill"/>,
/// <see cref="GBZeroFill"/> and <see cref="ThreadLocalZeroFill"/> have a virtual size but no
/// file content, so their recorded offset does not point at bytes belonging to them.
/// </remarks>
public enum MachOSectionType : byte
{
    /// <summary>Regular section (<c>S_REGULAR</c>).</summary>
    Regular = 0x0,
    /// <summary>Zero-filled on demand, occupying no file space (<c>S_ZEROFILL</c>).</summary>
    ZeroFill = 0x1,
    /// <summary>Only literal C strings (<c>S_CSTRING_LITERALS</c>).</summary>
    CStringLiterals = 0x2,
    /// <summary>Only 4-byte literals (<c>S_4BYTE_LITERALS</c>).</summary>
    FourByteLiterals = 0x3,
    /// <summary>Only 8-byte literals (<c>S_8BYTE_LITERALS</c>).</summary>
    EightByteLiterals = 0x4,
    /// <summary>Only pointers to literals (<c>S_LITERAL_POINTERS</c>).</summary>
    LiteralPointers = 0x5,
    /// <summary>Only non-lazy symbol pointers (<c>S_NON_LAZY_SYMBOL_POINTERS</c>).</summary>
    NonLazySymbolPointers = 0x6,
    /// <summary>Only lazy symbol pointers (<c>S_LAZY_SYMBOL_POINTERS</c>).</summary>
    LazySymbolPointers = 0x7,
    /// <summary>Only symbol stubs; the stub size is in <c>reserved2</c> (<c>S_SYMBOL_STUBS</c>).</summary>
    SymbolStubs = 0x8,
    /// <summary>Only function pointers for initialization (<c>S_MOD_INIT_FUNC_POINTERS</c>).</summary>
    ModInitFuncPointers = 0x9,
    /// <summary>Only function pointers for termination (<c>S_MOD_TERM_FUNC_POINTERS</c>).</summary>
    ModTermFuncPointers = 0xa,
    /// <summary>Only symbols that are to be coalesced (<c>S_COALESCED</c>).</summary>
    Coalesced = 0xb,
    /// <summary>Zero-filled on demand, and may be larger than 4GB (<c>S_GB_ZEROFILL</c>).</summary>
    GBZeroFill = 0xc,
    /// <summary>Only pairs of function pointers for interposing (<c>S_INTERPOSING</c>).</summary>
    Interposing = 0xd,
    /// <summary>Only 16-byte literals (<c>S_16BYTE_LITERALS</c>).</summary>
    SixteenByteLiterals = 0xe,
    /// <summary>Contains DTrace object format data (<c>S_DTRACE_DOF</c>).</summary>
    DtraceDof = 0xf,
    /// <summary>Only lazy symbol pointers to lazy loaded dylibs (<c>S_LAZY_DYLIB_SYMBOL_POINTERS</c>).</summary>
    LazyDylibSymbolPointers = 0x10,
    /// <summary>Thread local data (<c>S_THREAD_LOCAL_REGULAR</c>).</summary>
    ThreadLocalRegular = 0x11,
    /// <summary>Thread local zero-filled data (<c>S_THREAD_LOCAL_ZEROFILL</c>).</summary>
    ThreadLocalZeroFill = 0x12,
    /// <summary>Thread local variable descriptors (<c>S_THREAD_LOCAL_VARIABLES</c>).</summary>
    ThreadLocalVariables = 0x13,
    /// <summary>Pointers to thread local variable descriptors (<c>S_THREAD_LOCAL_VARIABLE_POINTERS</c>).</summary>
    ThreadLocalVariablePointers = 0x14,
    /// <summary>Functions to call to initialize a thread local variable (<c>S_THREAD_LOCAL_INIT_FUNCTION_POINTERS</c>).</summary>
    ThreadLocalInitFunctionPointers = 0x15,
    /// <summary>32-bit offsets to initializers (<c>S_INIT_FUNC_OFFSETS</c>).</summary>
    InitFuncOffsets = 0x16,
}
