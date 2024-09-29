// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace LibObjectFile.Collections;

/// <summary>
/// Represents a pooled span that can be created from a stack or heap memory.
/// </summary>
/// <typeparam name="T">The type of the elements in the span.</typeparam>
public readonly ref struct PooledSpan<T> where T : unmanaged
{
    private readonly Span<T> _span;
    private readonly byte[]? _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledSpan{T}"/> struct using a stack memory span.
    /// </summary>
    /// <param name="span">The stack memory span.</param>
    /// <param name="size">The size of the span in bytes.</param>
    private PooledSpan(Span<byte> span, int size)
    {
        _span = MemoryMarshal.Cast<byte, T>(span.Slice(0, size));
        _buffer = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledSpan{T}"/> struct using a heap memory span.
    /// </summary>
    /// <param name="buffer">The heap memory buffer.</param>
    /// <param name="size">The size of the span in bytes.</param>
    private PooledSpan(byte[] buffer, int size)
    {
        _span = MemoryMarshal.Cast<byte, T>(new(buffer, 0, size));
        _buffer = buffer;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PooledSpan{T}"/> struct with the specified number of elements.
    /// </summary>
    /// <param name="count">The number of elements in the span.</param>
    /// <returns>A new instance of the <see cref="PooledSpan{T}"/> struct.</returns>
    public static unsafe PooledSpan<T> Create(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        var size = count * sizeof(T);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        return new PooledSpan<T>(buffer, size);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PooledSpan{T}"/> struct with the specified number of elements, using a stack memory span if possible.
    /// </summary>
    /// <param name="stackSpan">The stack memory span.</param>
    /// <param name="count">The number of elements in the span.</param>
    /// <returns>A new instance of the <see cref="PooledSpan{T}"/> struct.</returns>
    public static unsafe PooledSpan<T> Create(Span<byte> stackSpan, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        var size = count * sizeof(T);
        return size <= stackSpan.Length ? new PooledSpan<T>(stackSpan, size) : Create(count);
    }

    /// <summary>
    /// Gets the span of elements.
    /// </summary>
    public Span<T> Span => _span;

    /// <summary>
    /// Gets the span of elements as bytes.
    /// </summary>
    public Span<byte> SpanAsBytes => MemoryMarshal.AsBytes(_span);

    /// <summary>
    /// Releases the underlying memory buffer if it was allocated on the heap.
    /// </summary>
    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
