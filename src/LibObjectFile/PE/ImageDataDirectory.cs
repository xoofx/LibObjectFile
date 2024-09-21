// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
/// <summary>
/// Data directory entry in the optional header of a Portable Executable (PE) file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ImageDataDirectory
{
    /// <summary>
    /// The relative virtual address of the data directory.
    /// </summary>
    public RVA RVA;

    /// <summary>
    /// The size of the data directory, in bytes.
    /// </summary>
    public uint Size;
}