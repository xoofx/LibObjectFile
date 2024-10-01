// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeaderCommonPart3
{
    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint LoaderFlags;

    /// <summary>
    /// The number of data-directory entries in the remainder of the optional header.
    /// </summary>
    public uint NumberOfRvaAndSizes;

    /// <summary>
    /// The data directories array, which contains the location and size of special tables in the file.
    /// </summary>
    public RawImageDataDirectoryArray DataDirectory;
}