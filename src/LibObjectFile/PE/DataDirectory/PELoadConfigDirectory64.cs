// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE64 file.
/// </summary>
public class PELoadConfigDirectory64 : PELoadConfigDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory64"/> class.
    /// </summary>
    public unsafe PELoadConfigDirectory64() : base(sizeof(PELoadConfigDirectoryData64))
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        SetRawDataSize(sizeof(PELoadConfigDirectoryData64));
    }

    /// <summary>
    /// Gets the 64-bit Load Configuration Directory.
    /// </summary>
    public ref PELoadConfigDirectoryData64 Data => ref Unsafe.As<byte, PELoadConfigDirectoryData64>(ref MemoryMarshal.GetArrayDataReference(RawData));
}