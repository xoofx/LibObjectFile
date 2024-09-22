// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
using System.Runtime.InteropServices;
using System;

namespace LibObjectFile.Utils;

internal static class DataUtils
{
    public static unsafe int ReadAt<TData32, TData64>(bool is32Bits, ref TData32 data32, ref TData64 data64, uint offset, Span<byte> destination)
        where TData32 : unmanaged
        where TData64 : unmanaged
    {
        if (is32Bits)
        {
            var endOffset = offset + destination.Length;
            if (endOffset > sizeof(TData32))
            {
                return 0;
            }

            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data32, 1));
            span.Slice((int)offset, (int)destination.Length).CopyTo(destination);
        }
        else
        {
            var endOffset = offset + destination.Length;
            if (endOffset > sizeof(TData64))
            {
                return 0;
            }

            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data64, 1));
            span.Slice((int)offset, (int)destination.Length).CopyTo(destination);
        }

        return destination.Length;
    }


    public static unsafe void WriteAt<TData32, TData64>(bool is32Bits, ref TData32 data32, ref TData64 data64, uint offset, ReadOnlySpan<byte> source)
        where TData32 : unmanaged
        where TData64 : unmanaged
    {
        if (is32Bits)
        {
            var endOffset = offset + source.Length;
            if (endOffset > sizeof(TData32))
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Source length is too big");
            }

            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data32, 1));
            source.CopyTo(span.Slice((int)offset, (int)source.Length));
        }
        else
        {
            var endOffset = offset + source.Length;
            if (endOffset > sizeof(TData64))
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Source length is too big");
            }

            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data64, 1));
            source.CopyTo(span.Slice((int)offset, (int)source.Length));
        }
    }
}