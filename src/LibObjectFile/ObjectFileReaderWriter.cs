// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.IO;

namespace LibObjectFile;

/// <summary>
/// Base class used for reading / writing an object file to/from a stream.
/// </summary>
public abstract class ObjectFileReaderWriter : VisitorContextBase
{
    private Stream _stream;
    private readonly byte[] _zeroBuffer = new byte[1024];

    internal ObjectFileReaderWriter(ObjectFileElement file, Stream stream) : this(file, stream, new DiagnosticBag())
    {
    }

    internal ObjectFileReaderWriter(ObjectFileElement file, Stream stream, DiagnosticBag diagnostics) : base(file, diagnostics)
    {
        _stream = stream;
        IsLittleEndian = true;
    }

    /// <summary>
    /// Gets or sets stream of the object file.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set => _stream = value;
    }

    public ulong Position
    {
        get => (ulong) Stream.Position;
        set => Stream.Seek((long)value, SeekOrigin.Begin);
    }

    public ulong Length
    {
        get => (ulong) Stream.Length;
    }

    /// <summary>
    /// Gets a boolean indicating if this reader is operating in read-only mode.
    /// </summary>
    public abstract bool KeepOriginalStreamForSubStreams { get; }

    public bool IsLittleEndian { get; protected set; }

    /// <summary>
    /// Reads from the <see cref="Stream"/> and current position to the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to receive the content of the read.</param>
    /// <param name="offset">The offset into the buffer.</param>
    /// <param name="count">The number of bytes to write from the buffer.</param>
    public int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

    public int Read(Span<byte> buffer) => Stream.Read(buffer);

    public void ReadExactly(Span<byte> buffer) => Stream.ReadExactly(buffer);

    /// <summary>
    /// Reads a null terminated UTF8 string from the stream.
    /// </summary>
    /// <returns><c>true</c> if the string was successfully read from the stream, false otherwise</returns>
    public string ReadStringUTF8NullTerminated()
    {
        return Stream.ReadStringUTF8NullTerminated();
    }
     
    /// <summary>
    /// Reads a null terminated UTF8 string from the stream.
    /// </summary>
    /// <param name="byteLength">The number of bytes to read including the null</param>
    /// <returns>A string</returns>
    public string ReadStringUTF8NullTerminated(uint byteLength)
    {
        return Stream.ReadStringUTF8NullTerminated(byteLength);
    }

    public byte ReadU8()
    {
        return Stream.ReadU8();
    }

    public sbyte ReadI8()
    {
        return Stream.ReadI8();
    }

    public short ReadI16()
    {
        return Stream.ReadI16(IsLittleEndian);
    }

    public ushort ReadU16()
    {
        return Stream.ReadU16(IsLittleEndian);
    }

    public int ReadI32()
    {
        return Stream.ReadI32(IsLittleEndian);
    }

    public uint ReadU32()
    {
        return Stream.ReadU32(IsLittleEndian);
    }
        
    public long ReadI64()
    {
        return Stream.ReadI64(IsLittleEndian);
    }

    public ulong ReadU64()
    {
        return Stream.ReadU64(IsLittleEndian);
    }

    public void WriteI8(sbyte value)
    {
        Stream.WriteI8(value);
    }

    public void WriteU8(byte value)
    {
        Stream.WriteU8(value);
    }
        
    public void WriteU16(ushort value)
    {
        Stream.WriteU16(IsLittleEndian, value);
    }

    public void WriteU32(uint value)
    {
        Stream.WriteU32(IsLittleEndian, value);
    }

    public void WriteU64(ulong value)
    {
        Stream.WriteU64(IsLittleEndian, value);
    }

    /// <summary>
    /// Writes a null terminated UTF8 string to the stream.
    /// </summary>
    public void WriteStringUTF8NullTerminated(string text)
    {
        Stream.WriteStringUTF8NullTerminated(text);
    }

    /// <summary>
    /// Tries to read an element of type <paramref name="{T}"/> with a specified size.
    /// </summary>
    /// <typeparam name="T">Type of the element to read.</typeparam>
    /// <param name="sizeToRead">Size of the element to read (might be smaller or bigger).</param>
    /// <param name="data">The data read.</param>
    /// <returns><c>true</c> if reading was successful. <c>false</c> otherwise.</returns>
    public unsafe bool TryReadData<T>(int sizeToRead, out T data) where T : unmanaged
        => Stream.TryReadData(sizeToRead, out data);

    /// <summary>
    /// Reads from the current <see cref="Stream"/> <see cref="size"/> bytes and return the data as
    /// a <see cref="SubStream"/> if <see cref="KeepOriginalStreamForSubStreams"/> is <c>false</c> otherwise as a 
    /// <see cref="MemoryStream"/>.
    /// </summary>
    /// <param name="size">Size of the data to read.</param>
    /// <returns>A <see cref="SubStream"/> if <see cref="KeepOriginalStreamForSubStreams"/> is <c>false</c> otherwise as a 
    /// <see cref="MemoryStream"/>.</returns>
    public Stream ReadAsStream(ulong size)
        => Stream.ReadAsStream(size, Diagnostics, KeepOriginalStreamForSubStreams);
    
    /// <summary>
    /// Writes to the <see cref="Stream"/> and current position from the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write the content from.</param>
    /// <param name="offset">The offset into the buffer.</param>
    /// <param name="count">The number of bytes to read from the buffer and write to the stream.</param>
    public void Write(byte[] buffer, int offset, int count)
    {
        Stream.Write(buffer, offset, count);
    }

    /// <summary>
    /// Writes count bytes with zero.
    /// </summary>
    /// <param name="count">The number of bytes to write with zero.</param>
    public void WriteZero(int count)
    {
        while (count > 0)
        {
            var size = Math.Min(count, _zeroBuffer.Length);
            Stream.Write(_zeroBuffer, 0, size);
            count -= size;
        }
    }

    /// <summary>
    /// Writes to the <see cref="Stream"/> and current position from the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write</param>
    public void Write(ReadOnlySpan<byte> buffer) => Stream.Write(buffer);

    /// <summary>
    /// Writes an element of type <paramref name="{T}"/> to the stream.
    /// </summary>
    /// <typeparam name="T">Type of the element to read.</typeparam>
    /// <param name="data">The data to write.</param>
    public unsafe void Write<T>(in T data) where T : unmanaged
    {
        fixed (void* pData = &data)
        {
            var span = new ReadOnlySpan<byte>(pData, sizeof(T));
            Stream.Write(span);
        }
    }

    /// <summary>
    /// Writes from the specified stream to the current <see cref="Stream"/> of this instance.
    /// The position of the input stream is set to 0 before writing and reset back to 0 after writing.
    /// </summary>
    /// <param name="inputStream">The input stream to read from and write to <see cref="Stream"/></param>
    /// <param name="size">The amount of data to read from the input stream (if == 0, by default, it will read the entire input stream)</param>
    /// <param name="bufferSize">The size of the intermediate buffer used to transfer the data.</param>
    public void Write(Stream inputStream, ulong size = 0, int bufferSize = 4096)
    {
        if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        inputStream.Seek(0, SeekOrigin.Begin);
        size = size == 0 ? (ulong)inputStream.Length : size;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        while (size != 0)
        {
            var sizeToRead = size >= (ulong)buffer.Length ? buffer.Length : (int)size;
            var sizeRead = inputStream.Read(buffer, 0, sizeToRead);
            if (sizeRead <= 0) break;

            Stream.Write(buffer, 0, sizeRead);
            size -= (ulong)sizeRead;
        }

        inputStream.Seek(0, SeekOrigin.Begin);
        if (size != 0)
        {
            throw new InvalidOperationException("Unable to write stream entirely");
        }
    }
}