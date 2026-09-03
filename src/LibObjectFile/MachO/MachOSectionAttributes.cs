// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Attributes of a section, held in the upper three bytes of the section <c>flags</c> field
/// (<c>SECTION_ATTRIBUTES</c>). The low byte holds the <see cref="MachOSectionType"/> instead.
/// </summary>
[Flags]
public enum MachOSectionAttributes : uint
{
    /// <summary>No attributes set.</summary>
    None = 0,
    /// <summary>Section contains only true machine instructions (<c>S_ATTR_PURE_INSTRUCTIONS</c>).</summary>
    PureInstructions = 0x80000000,
    /// <summary>Section contains coalesced symbols that are not to be in the table of contents (<c>S_ATTR_NO_TOC</c>).</summary>
    NoToc = 0x40000000,
    /// <summary>Static symbols in this section can be stripped (<c>S_ATTR_STRIP_STATIC_SYMS</c>).</summary>
    StripStaticSyms = 0x20000000,
    /// <summary>No dead stripping (<c>S_ATTR_NO_DEAD_STRIP</c>).</summary>
    NoDeadStrip = 0x10000000,
    /// <summary>Blocks are live if they reference live blocks (<c>S_ATTR_LIVE_SUPPORT</c>).</summary>
    LiveSupport = 0x08000000,
    /// <summary>Used in code that can be modified, such as dyld stubs (<c>S_ATTR_SELF_MODIFYING_CODE</c>).</summary>
    SelfModifyingCode = 0x04000000,
    /// <summary>A debug section, which the linker treats specially (<c>S_ATTR_DEBUG</c>).</summary>
    Debug = 0x02000000,
    /// <summary>Section contains some machine instructions (<c>S_ATTR_SOME_INSTRUCTIONS</c>).</summary>
    SomeInstructions = 0x00000400,
    /// <summary>Section has external relocation entries (<c>S_ATTR_EXT_RELOC</c>).</summary>
    ExternalReloc = 0x00000200,
    /// <summary>Section has local relocation entries (<c>S_ATTR_LOC_RELOC</c>).</summary>
    LocalReloc = 0x00000100,
}
