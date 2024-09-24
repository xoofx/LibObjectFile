// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Utils;

internal static class DataUtils
{
    public static int ReadAt(ReadOnlySpan<byte> source, uint offset, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, (uint)source.Length);

        var lengthToCopy = Math.Min(source.Length - (int)offset, destination.Length);
        source.Slice((int)offset, lengthToCopy).CopyTo(destination);

        return lengthToCopy;
    }

    public static unsafe void WriteAt(Span<byte> destination, uint offset, ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, (uint)destination.Length);

        if (source.Length > (destination.Length - (int)offset))
        {
            throw new ArgumentOutOfRangeException(nameof(source), $"The source buffer is too large to fit at the offset {offset} in the destination buffer");
        }

        source.CopyTo(destination.Slice((int)offset, source.Length));
    }
}