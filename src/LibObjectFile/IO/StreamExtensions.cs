// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace LibObjectFile.IO;

public static class StreamExtensions
{
    public static byte ReadU8(this Stream stream)
    {
        int nextValue = stream.ReadByte();
        if (nextValue < 0) throw new EndOfStreamException();
        return (byte)nextValue;
    }

    public static void WriteU8(this Stream stream, byte value)
    {
        stream.WriteByte(value);
    }

    public static sbyte ReadI8(this Stream stream)
    {
        int nextValue = stream.ReadByte();
        if (nextValue < 0) throw new EndOfStreamException();
        return (sbyte)nextValue;
    }

    public static void WriteI8(this Stream stream, sbyte value)
    {
        stream.WriteByte((byte)value);
    }

    /// <summary>
    /// Reads a null terminated UTF8 string from the stream.
    /// </summary>
    /// <returns><c>true</c> if the string was successfully read from the stream, false otherwise</returns>
    public static string ReadStringUTF8NullTerminated(this Stream stream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent((int)128);
        int textLength = 0;
        try
        {
            while (true)
            {
                // TODO: Optimize this by reading a block of bytes
                int nextByte = stream.ReadByte();
                if (nextByte < 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while trying to read a null terminated UTF8 string");
                }

                if (nextByte == 0)
                {
                    break;
                }

                if (textLength >= buffer.Length)
                {
                    var newBuffer = ArrayPool<byte>.Shared.Rent((int)textLength * 2);
                    Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                buffer[textLength++] = (byte)nextByte;
            }

            return Encoding.UTF8.GetString(buffer, 0, textLength);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a null terminated UTF8 string from the stream.
    /// </summary>
    /// <param name="byteLength">The number of bytes to read including the null</param>
    /// <returns>A string</returns>
    public static string ReadStringUTF8NullTerminated(this Stream stream, uint byteLength)
    {
        if (byteLength == 0) return string.Empty;

        var buffer = ArrayPool<byte>.Shared.Rent((int)byteLength);
        try
        {
            var dataLength = stream.Read(buffer, 0, (int)byteLength);
            if (dataLength < 0) throw new EndOfStreamException("Unexpected end of stream while trying to read data");

            var byteReadLength = (uint)dataLength;
            if (byteReadLength != byteLength) throw new EndOfStreamException($"Not enough data read {byteReadLength} bytes while expecting to read {byteLength} bytes");

            var isNullTerminated = buffer[byteReadLength - 1] == 0;

            var text = Encoding.UTF8.GetString(buffer, 0, (int)(isNullTerminated ? byteReadLength - 1 : byteReadLength));
            return text;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    public static short ReadI16(this Stream stream, bool isLittleEndian)
    {
        return (short)ReadU16(stream, isLittleEndian);
    }

    public static ushort ReadU16(this Stream stream, bool isLittleEndian)
    {
        ushort value = 0;
        int nextValue = stream.ReadByte();
        if (nextValue < 0) throw new EndOfStreamException();
        value = (byte)nextValue;

        nextValue = stream.ReadByte();
        if (nextValue < 0) throw new EndOfStreamException();
        value = (ushort)((nextValue << 8) | (byte)value);

        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    public static int ReadI32(this Stream stream, bool isLittleEndian)
    {
        return (int)ReadU32(stream, isLittleEndian);
    }

    public static unsafe uint ReadU32(this Stream stream, bool isLittleEndian)
    {
        uint value = 0;
        var span = new Span<byte>((byte*)&value, sizeof(uint));
        if (stream.Read(span) != sizeof(uint))
        {
            throw new EndOfStreamException();
        }

        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        return value;
    }
    public static long ReadI64(this Stream stream, bool isLittleEndian)
    {
        return (long)ReadU64(stream, isLittleEndian);
    }

    public static unsafe ulong ReadU64(this Stream stream, bool isLittleEndian)
    {
        ulong value = 0;
        var span = new Span<byte>((byte*)&value, sizeof(ulong));
        if (stream.Read(span) != sizeof(ulong))
        {
            throw new EndOfStreamException();
        }

        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        return value;
    }

    public static unsafe void WriteU16(this Stream stream, bool isLittleEndian, ushort value)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        var span = new Span<byte>((byte*)&value, sizeof(ushort));
        stream.Write(span);
    }

    public static void WriteI32(this Stream stream, bool isLittleEndian, int value)
    {
        WriteU32(stream, isLittleEndian, (uint)value);
    }

    public static unsafe void WriteU32(this Stream stream, bool isLittleEndian, uint value)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        var span = new Span<byte>((byte*)&value, sizeof(uint));
        stream.Write(span);
    }

    public static unsafe void WriteU64(this Stream stream, bool isLittleEndian, ulong value)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        var span = new Span<byte>((byte*)&value, sizeof(ulong));
        stream.Write(span);
    }

    /// <summary>
    /// Writes a null terminated UTF8 string to the stream.
    /// </summary>
    public static void WriteStringUTF8NullTerminated(this Stream stream, string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));

        int byteLength = Encoding.UTF8.GetByteCount(text);
        var buffer = ArrayPool<byte>.Shared.Rent(byteLength + 1);
        try
        {
            Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
            buffer[byteLength] = 0;
            stream.Write(buffer, 0, byteLength + 1);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Tries to read an element of type <paramref name="{T}"/> with a specified size.
    /// </summary>
    /// <typeparam name="T">Type of the element to read.</typeparam>
    /// <param name="sizeToRead">Size of the element to read (might be smaller or bigger).</param>
    /// <param name="data">The data read.</param>
    /// <returns><c>true</c> if reading was successful. <c>false</c> otherwise.</returns>
    public static unsafe bool TryReadData<T>(this Stream stream, int sizeToRead, out T data) where T : unmanaged
    {
        if (sizeToRead <= 0) throw new ArgumentOutOfRangeException(nameof(sizeToRead));

        int dataByteCount = sizeof(T);
        int byteRead;

        // If we are requested to read more data than the sizeof(T)
        // we need to read it to an intermediate buffer before transferring it to T data
        if (sizeToRead > dataByteCount)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(sizeToRead);
            var span = new Span<byte>(buffer, 0, sizeToRead);
            byteRead = stream.Read(span);
            data = MemoryMarshal.Cast<byte, T>(span)[0];
            ArrayPool<byte>.Shared.Return(buffer);
        }
        else
        {
            // Clear the data if the size requested is less than the expected struct to read
            if (sizeToRead < dataByteCount)
            {
                data = default;
            }

            Unsafe.SkipInit(out data);
            byteRead = stream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)).Slice(0, sizeToRead));
        }
        return byteRead == sizeToRead;
    }

    public static SubStream ReadAsSubStream(this Stream stream, ulong size, DiagnosticBag diagnostics)
    {
        var position = stream.Position;
        if (position + (long)size > stream.Length)
        {
            if (position < stream.Length)
            {
                size = stream.Position < stream.Length ? (ulong)(stream.Length - stream.Position) : 0;
                diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Expecting to slice {size} bytes at offset {position} while remaining length is {size}");
            }
            else
            {
                position = stream.Length;
                size = 0;
                diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Position of slice {position} is outside of the stream length {stream.Length} in bytes");
            }
        }

        return new SubStream(stream, position, (long)size);
    }

    public static MemoryStream ReadAsMemoryStream(this Stream stream, ulong size, DiagnosticBag diagnostics)
    {
        var memoryStream = new MemoryStream((int)size);
        if (size == 0) return memoryStream;

        memoryStream.SetLength((long)size);

        var buffer = memoryStream.GetBuffer();
        var span = new Span<byte>(buffer, 0, (int)size);
        var readSize = stream.Read(span);

        if ((int)size != readSize)
        {
            diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Expecting to read {size} bytes at offset {stream.Position}");
        }

        return memoryStream;
    }

    /// <summary>
    /// Reads from the current <see cref="Stream"/> <see cref="size"/> bytes and return the data as
    /// a <see cref="SubStream"/> if <see cref="keepOriginalStreamForSubStreams"/> is <c>false</c> otherwise as a 
    /// <see cref="MemoryStream"/>.
    /// </summary>
    /// <param name="size">Size of the data to read.</param>
    /// <returns>A <see cref="SubStream"/> if <see cref="keepOriginalStreamForSubStreams"/> is <c>false</c> otherwise as a 
    /// <see cref="MemoryStream"/>.</returns>
    public static Stream ReadAsStream(this Stream sourceStream, ulong size, DiagnosticBag diagnostics, bool keepOriginalStreamForSubStreams)
    {
        if (keepOriginalStreamForSubStreams)
        {
            var stream = sourceStream.ReadAsSubStream(size, diagnostics);
            sourceStream.Seek(stream.Length, SeekOrigin.Current);
            return stream;
        }

        return sourceStream.ReadAsMemoryStream(size, diagnostics);
    }
}