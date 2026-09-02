// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The symbol table load command (<c>symtab_command</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawSymtabCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint SymbolOffset;
    public uint SymbolCount;
    public uint StringOffset;
    public uint StringSize;
}
