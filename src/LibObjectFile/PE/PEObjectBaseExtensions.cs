// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

public static class PEObjectBaseExtensions
{
    public static unsafe T ReadAt<T>(this PEObjectBase obj, uint offset) where T : unmanaged
    {
        T value = default;
        
        int read = obj.ReadAt(offset, MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), sizeof(T)));
        if (read != sizeof(T))
        {
            throw new InvalidOperationException($"Failed to read {typeof(T).Name} at offset {offset} in {obj}");
        }

        return value;
    }

    public static unsafe void WriteAt<T>(this PEObjectBase obj, uint offset, T value) where T : unmanaged
    {
        obj.WriteAt(offset, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref value), sizeof(T)));
    }
}