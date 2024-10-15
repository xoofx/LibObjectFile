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
/// Represents a batch data writer for writing elements of type <typeparamref name="TData"/> to a stream.
/// </summary>
/// <typeparam name="TData">The type of the elements to write.</typeparam>
public unsafe ref struct BatchDataWriter<TData> where TData : unmanaged
{
    private readonly Stream _stream;
    private readonly int _count;
    private readonly byte[] _buffer;
    private readonly ref TData _firstValue;
    private int _index;
    private const int BatchSize = 1024; // TODO: could be made configurable

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDataWriter{TData}"/> struct.
    /// </summary>
    /// <param name="stream">The stream to write the data to.</param>
    /// <param name="count">The total number of elements to write.</param>
    public BatchDataWriter(Stream stream, int count)
    {
        _stream = stream;
        _count = count;
        var size = sizeof(TData) * Math.Min(count, BatchSize);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        _firstValue = ref Unsafe.As<byte, TData>(ref MemoryMarshal.GetArrayDataReference(buffer));
        _buffer = buffer;
    }

    /// <summary>
    /// Gets a value indicating whether there are more elements to write.
    /// </summary>
    /// <returns><c>true</c> if there are more elements to write; otherwise, <c>false</c>.</returns>
    public bool HasNext() => _index < _count;

    /// <summary>
    /// Writes the specified value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(in TData value)
    {
        var index = _index;
        var count = _count;
        if (index >= count)
        {
            throw new InvalidOperationException("No more elements to write");
        }

        var remaining = index & (BatchSize - 1);
        if (remaining == 0 && index > 0)
        {
            _stream.Write(_buffer, 0, BatchSize * sizeof(TData));
        }

        Unsafe.Add(ref _firstValue, remaining) = value;
        _index = index + 1;
    }

    /// <summary>
    /// Releases the resources used by the <see cref="BatchDataWriter{TData}"/>.
    /// </summary>
    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer != null)
        {
            var remaining = _count & (BatchSize - 1);
            if (remaining != 0)
            {
                var sizeToWrite = remaining * sizeof(TData);
                _stream.Write(buffer, 0, sizeToWrite);
            }
            else if (_count > 0)
            {
                _stream.Write(buffer, 0, BatchSize * sizeof(TData));
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
