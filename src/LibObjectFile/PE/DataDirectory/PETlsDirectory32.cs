// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the 32-bit Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public sealed class PETlsDirectory32 : PETlsDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    public unsafe PETlsDirectory32() : base(sizeof(PETlsDirectoryData32))
    {
    }
    
    public override unsafe int RawDataSize => sizeof(PETlsDirectoryData32);

    /// <summary>
    /// Gets the 32-bit Thread Local Storage (TLS) directory.
    /// </summary>
    public ref PETlsDirectoryData32 Data => ref Unsafe.As<byte, PETlsDirectoryData32>(ref MemoryMarshal.GetArrayDataReference(RawData));

    public override unsafe void SetRawDataSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, sizeof(PETlsDirectoryData32));
        base.SetRawDataSize(value);
    }
}