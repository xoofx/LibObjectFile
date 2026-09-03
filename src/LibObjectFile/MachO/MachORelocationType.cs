// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Relocation types for 32-bit Intel and the other architectures using the generic set.
/// </summary>
/// <remarks>
/// The numbering restarts for every architecture, so a value only means something once the
/// image's <see cref="MachOCpuType"/> is known.
/// </remarks>
public enum MachOGenericRelocationType : byte
{
    /// <summary>The relocated field takes the symbol's address (<c>GENERIC_RELOC_VANILLA</c>).</summary>
    Vanilla = 0,
    /// <summary>The second half of a pair, following the entry it belongs to (<c>GENERIC_RELOC_PAIR</c>).</summary>
    Pair = 1,
    /// <summary>The difference between two section-relative addresses (<c>GENERIC_RELOC_SECTDIFF</c>).</summary>
    SectionDifference = 2,
    /// <summary>A prebound lazy pointer (<c>GENERIC_RELOC_PB_LA_PTR</c>).</summary>
    PreboundLazyPointer = 3,
    /// <summary>A section difference where the subtrahend is local (<c>GENERIC_RELOC_LOCAL_SECTDIFF</c>).</summary>
    LocalSectionDifference = 4,
    /// <summary>A thread local variable reference (<c>GENERIC_RELOC_TLV</c>).</summary>
    ThreadLocalVariable = 5,
}

/// <summary>
/// Relocation types for 64-bit Intel.
/// </summary>
public enum MachOX86_64RelocationType : byte
{
    /// <summary>An absolute address (<c>X86_64_RELOC_UNSIGNED</c>).</summary>
    Unsigned = 0,
    /// <summary>A signed 32-bit displacement (<c>X86_64_RELOC_SIGNED</c>).</summary>
    Signed = 1,
    /// <summary>The target of a call or jump (<c>X86_64_RELOC_BRANCH</c>).</summary>
    Branch = 2,
    /// <summary>A load through the global offset table (<c>X86_64_RELOC_GOT_LOAD</c>).</summary>
    GotLoad = 3,
    /// <summary>Any other global offset table reference (<c>X86_64_RELOC_GOT</c>).</summary>
    Got = 4,
    /// <summary>The subtrahend of a difference, paired with the entry after it (<c>X86_64_RELOC_SUBTRACTOR</c>).</summary>
    Subtractor = 5,
    /// <summary>A signed displacement with an implicit addend of one (<c>X86_64_RELOC_SIGNED_1</c>).</summary>
    Signed1 = 6,
    /// <summary>A signed displacement with an implicit addend of two (<c>X86_64_RELOC_SIGNED_2</c>).</summary>
    Signed2 = 7,
    /// <summary>A signed displacement with an implicit addend of four (<c>X86_64_RELOC_SIGNED_4</c>).</summary>
    Signed4 = 8,
    /// <summary>A thread local variable reference (<c>X86_64_RELOC_TLV</c>).</summary>
    ThreadLocalVariable = 9,
}

/// <summary>
/// Relocation types for 64-bit ARM.
/// </summary>
public enum MachOArm64RelocationType : byte
{
    /// <summary>An absolute address (<c>ARM64_RELOC_UNSIGNED</c>).</summary>
    Unsigned = 0,
    /// <summary>The subtrahend of a difference, paired with the entry after it (<c>ARM64_RELOC_SUBTRACTOR</c>).</summary>
    Subtractor = 1,
    /// <summary>The target of a 26-bit branch (<c>ARM64_RELOC_BRANCH26</c>).</summary>
    Branch26 = 2,
    /// <summary>The page of a target, for <c>adrp</c> (<c>ARM64_RELOC_PAGE21</c>).</summary>
    Page21 = 3,
    /// <summary>The offset within a page (<c>ARM64_RELOC_PAGEOFF12</c>).</summary>
    PageOffset12 = 4,
    /// <summary>The page of a global offset table entry (<c>ARM64_RELOC_GOT_LOAD_PAGE21</c>).</summary>
    GotLoadPage21 = 5,
    /// <summary>The offset of a global offset table entry within its page (<c>ARM64_RELOC_GOT_LOAD_PAGEOFF12</c>).</summary>
    GotLoadPageOffset12 = 6,
    /// <summary>A pointer to a global offset table entry (<c>ARM64_RELOC_POINTER_TO_GOT</c>).</summary>
    PointerToGot = 7,
    /// <summary>The page of a thread local variable pointer (<c>ARM64_RELOC_TLVP_LOAD_PAGE21</c>).</summary>
    ThreadLocalPage21 = 8,
    /// <summary>The offset of a thread local variable pointer within its page (<c>ARM64_RELOC_TLVP_LOAD_PAGEOFF12</c>).</summary>
    ThreadLocalPageOffset12 = 9,
    /// <summary>An addend applied to the entry after it (<c>ARM64_RELOC_ADDEND</c>).</summary>
    Addend = 10,
}
