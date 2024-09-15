// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

#pragma warning disable CS0649

/// <summary>
/// Represents the COFF (Common Object File Format) header in a Portable Executable (PE) file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ImageCoffHeader
{
    /// <summary>
    /// The machine type that the file is intended for.
    /// </summary>
    public System.Reflection.PortableExecutable.Machine Machine;

    /// <summary>
    /// The number of sections in the file.
    /// </summary>
    public ushort NumberOfSections;

    /// <summary>
    /// The low 32 bits of the time stamp indicating when the file was created.
    /// </summary>
    public uint TimeDateStamp;

    /// <summary>
    /// The file pointer to the COFF symbol table. This is zero if no symbol table is present.
    /// </summary>
    public uint PointerToSymbolTable;

    /// <summary>
    /// The number of entries in the symbol table.
    /// </summary>
    public uint NumberOfSymbols;

    /// <summary>
    /// The size of the optional header, which is required for executable files but not for object files.
    /// </summary>
    public ushort SizeOfOptionalHeader;

    /// <summary>
    /// The characteristics of the file that define its properties, such as whether it's an executable, a DLL, etc.
    /// </summary>
    public System.Reflection.PortableExecutable.Characteristics Characteristics;
}