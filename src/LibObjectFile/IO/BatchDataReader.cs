// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.IO;

/// <summary>
/// Represents a batch data reader for reading elements of type <typeparamref name="TData"/> from a stream.
/// </summary>
/// <typeparam name="TData">The type of the elements to read.</typeparam>
public unsafe ref struct BatchDataReader<TData> where TData : unmanaged
{
    private readonly Stream _stream;
    private readonly int _count;
    private readonly byte[] _buffer;
    private readonly ref TData _firstValue;
    private int _index;
    private const int BatchSize = 1024; // TODO: could be made configurable

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDataReader{TData}"/> struct.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="count">The total number of elements to read.</param>
    public BatchDataReader(Stream stream, int count)
    {
        _stream = stream;
        _count = count;
        var size = sizeof(TData) * Math.Min(count, BatchSize);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        _firstValue = ref Unsafe.As<byte, TData>(ref MemoryMarshal.GetArrayDataReference(buffer));
        _buffer = buffer;
    }

    /// <summary>
    /// Checks if there are more elements to read.
    /// </summary>
    /// <returns><c>true</c> if there are more elements to read; otherwise, <c>false</c>.</returns>
    public bool HasNext() => _index < _count;

    /// <summary>
    /// Reads the next element from the stream.
    /// </summary>
    /// <returns>A reference to the next element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there are no more elements to read.</exception>
    public ref TData Read()
    {
        var index = _index;
        var count = _count;
        if (index >= count)
        {
            throw new InvalidOperationException("No more elements to read");
        }

        var remaining = index & (BatchSize - 1);
        _index = index + 1;
        if (remaining == 0)
        {
            var sizeToRead = Math.Min(count - index, BatchSize) * sizeof(TData);
            int read = _stream.Read(_buffer, 0, sizeToRead);
            if (read != sizeToRead)
            {
                throw new EndOfStreamException($"Not enough data to read at position {_stream.Position}");
            }
        }

        ref var value = ref Unsafe.Add(ref _firstValue, remaining);
        return ref value;
    }

    /// <summary>
    /// Releases the resources used by the <see cref="BatchDataReader{TData}"/>.
    /// </summary>
    public void Dispose()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}
