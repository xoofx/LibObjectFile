// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The dynamic symbol table load command (<c>dysymtab_command</c>).
/// </summary>
/// <remarks>
/// The first three pairs are index and count into the symbol table proper, which the linker
/// sorts into local, external and undefined runs. The rest are file offsets to tables of their
/// own, each paired with an entry count rather than a byte size.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawDysymtabCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint LocalSymbolIndex;
    public uint LocalSymbolCount;
    public uint ExternalSymbolIndex;
    public uint ExternalSymbolCount;
    public uint UndefinedSymbolIndex;
    public uint UndefinedSymbolCount;
    public uint TableOfContentsOffset;
    public uint TableOfContentsCount;
    public uint ModuleTableOffset;
    public uint ModuleTableCount;
    public uint ExternalReferenceOffset;
    public uint ExternalReferenceCount;
    public uint IndirectSymbolOffset;
    public uint IndirectSymbolCount;
    public uint ExternalRelocationOffset;
    public uint ExternalRelocationCount;
    public uint LocalRelocationOffset;
    public uint LocalRelocationCount;
}
