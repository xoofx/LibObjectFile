// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

/// <summary>
/// Represents the optional header in a PE (Portable Executable) file format (32-bit).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeaderCommonPart1
{
    /// <summary>
    /// The magic number, which identifies the file format. Expected to be 0x10b for PE32.
    /// </summary>
    public PEOptionalHeaderMagic Magic;

    /// <summary>
    /// The major version number of the linker.
    /// </summary>
    public byte MajorLinkerVersion;

    /// <summary>
    /// The minor version number of the linker.
    /// </summary>
    public byte MinorLinkerVersion;

    /// <summary>
    /// The size of the code (text) section, in bytes.
    /// </summary>
    public uint SizeOfCode;

    /// <summary>
    /// The size of the initialized data section, in bytes.
    /// </summary>
    public uint SizeOfInitializedData;

    /// <summary>
    /// The size of the uninitialized data section, in bytes.
    /// </summary>
    public uint SizeOfUninitializedData;

    /// <summary>
    /// The address of the entry point relative to the image base when the executable starts.
    /// </summary>
    public uint AddressOfEntryPoint;

    /// <summary>
    /// The address relative to the image base of the beginning of the code section.
    /// </summary>
    public uint BaseOfCode;
}