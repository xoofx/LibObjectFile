// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeaderSize32
{
    /// <summary>
    /// The size of the stack to reserve, in bytes.
    /// </summary>
    public uint SizeOfStackReserve;

    /// <summary>
    /// The size of the stack to commit, in bytes.
    /// </summary>
    public uint SizeOfStackCommit;

    /// <summary>
    /// The size of the local heap space to reserve, in bytes.
    /// </summary>
    public uint SizeOfHeapReserve;

    /// <summary>
    /// The size of the local heap space to commit, in bytes.
    /// </summary>
    public uint SizeOfHeapCommit;
}