// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeaderSize64
{
    /// <summary>
    /// The size of the stack to reserve, in bytes.
    /// </summary>
    public ulong SizeOfStackReserve;

    /// <summary>
    /// The size of the stack to commit, in bytes.
    /// </summary>
    public ulong SizeOfStackCommit;

    /// <summary>
    /// The size of the local heap space to reserve, in bytes.
    /// </summary>
    public ulong SizeOfHeapReserve;

    /// <summary>
    /// The size of the local heap space to commit, in bytes.
    /// </summary>
    public ulong SizeOfHeapCommit;
}