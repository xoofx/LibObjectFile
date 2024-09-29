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
public struct PECoffHeader
{
    /// <summary>
    /// The machine type that the file is intended for.
    /// </summary>
    public System.Reflection.PortableExecutable.Machine Machine;

    internal ushort _NumberOfSections;

    /// <summary>
    /// The number of sections in the file.
    /// </summary>
    /// <remarks>
    /// This value is readonly as it will be updated by <see cref="PEFile.UpdateLayout(LibObjectFile.Diagnostics.DiagnosticBag)"/> and the number of sections will be computed from the sections list.
    /// </remarks>
    public ushort NumberOfSections => _NumberOfSections;

    /// <summary>
    /// The low 32 bits of the time stamp indicating when the file was created.
    /// </summary>
    public uint TimeDateStamp;

    internal uint _PointerToSymbolTable;

    /// <summary>
    /// The file pointer to the COFF symbol table. This is zero if no symbol table is present.
    /// </summary>
    /// <remarks>This value is readonly and is zero for a PE Image file.</remarks>
    public uint PointerToSymbolTable => _PointerToSymbolTable;

    internal uint _NumberOfSymbols;

    /// <summary>
    /// The number of entries in the symbol table.
    /// </summary>
    /// <remarks>This value is readonly and is zero for a PE Image file.</remarks>
    public uint NumberOfSymbols => _NumberOfSymbols;

    internal ushort _SizeOfOptionalHeader;

    /// <summary>
    /// The size of the optional header, which is required for executable files but not for object files.
    /// </summary>
    /// <remarks>
    /// This value is readonly as it will be updated automatically when reading or updating the layout of the PE file.
    /// </remarks>
    public ushort SizeOfOptionalHeader => _SizeOfOptionalHeader;

    /// <summary>
    /// The characteristics of the file that define its properties, such as whether it's an executable, a DLL, etc.
    /// </summary>
    public System.Reflection.PortableExecutable.Characteristics Characteristics;
}