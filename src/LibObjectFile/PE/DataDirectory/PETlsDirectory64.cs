// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the 64-bit Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public sealed class PETlsDirectory64 : PETlsDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    public unsafe PETlsDirectory64() : base(sizeof(PETlsDirectoryData64))
    {
    }
    
    public override unsafe int RawDataSize => sizeof(PETlsDirectoryData64);

    /// <summary>
    /// Gets the 64-bit Thread Local Storage (TLS) directory.
    /// </summary>
    public ref PETlsDirectoryData64 Data => ref Unsafe.As<byte, PETlsDirectoryData64>(ref MemoryMarshal.GetArrayDataReference(RawData));

    public override unsafe void SetRawDataSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, sizeof(PETlsDirectoryData64));
        base.SetRawDataSize(value);
    }
}