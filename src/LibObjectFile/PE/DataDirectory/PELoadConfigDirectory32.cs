// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE32 file.
/// </summary>
public class PELoadConfigDirectory32 : PELoadConfigDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory32"/> class.
    /// </summary>
    public unsafe PELoadConfigDirectory32() : base(sizeof(PELoadConfigDirectoryData32))
    {
        SetRawDataSize(sizeof(PELoadConfigDirectoryData32));
    }

    /// <summary>
    /// Gets the 32-bit Load Configuration Directory.
    /// </summary>
    public ref PELoadConfigDirectoryData32 Data => ref Unsafe.As<byte, PELoadConfigDirectoryData32>(ref MemoryMarshal.GetArrayDataReference(RawData));
}