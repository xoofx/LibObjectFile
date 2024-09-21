// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LibObjectFile.PE;

public static class PESectionDataExtensions
{
    public static string ReadAsciiString(this PEStreamSectionData sectionData, uint offset)
    {
        var stream = sectionData.Stream;
        if (offset >= stream.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"The offset {offset} is out of range in the stream > {stream.Length}");
        }

        stream.Position = offset;
        return ReadAsciiStringInternal(stream, false, out _);
    }

    public static PEImportHintName ReadHintName(this PEStreamSectionData sectionData, uint offset)
    {
        var stream = sectionData.Stream;
        if (offset >= stream.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"The offset {offset} is out of range in the stream > {stream.Length}");
        }
        stream.Position = offset;
        var name = ReadAsciiStringInternal(stream, true, out var hint);
        return new PEImportHintName(hint, name);
    }

    public static PEAsciiStringLink WriteAsciiString(this PEStreamSectionData streamData, string value)
    {
        var position = WriteAsciiString(streamData.Stream, value);
        if (position > uint.MaxValue)
        {
            throw new InvalidOperationException("The position is too large to be stored in a uint");
        }

        return new PEAsciiStringLink(streamData, (uint)position);
    }

    public static PEImportHintNameLink WriteHintName(this PEStreamSectionData streamData, PEImportHintName hintName)
    {
        var position = WriteHintName(streamData.Stream, hintName);
        if (position > uint.MaxValue)
        {
            throw new InvalidOperationException("The position is too large to be stored in a uint");
        }

        return new PEImportHintNameLink(streamData, (uint)position);
    }
    
    public static long WriteAsciiString(this Stream stream, string value)
    {
        var position = stream.Position;
        WriteAsciiStringInternal(stream, false, 0, value);
        return position;
    }

    public static long WriteHintName(this Stream stream, PEImportHintName hintName)
    {
        if (hintName.Name is null) throw new ArgumentNullException(nameof(hintName), "The name of the import cannot be null");
        var position = stream.Position;
        WriteAsciiStringInternal(stream, true, hintName.Hint, hintName.Name);
        return position;
    }
    
    private static void WriteAsciiStringInternal(Stream stream, bool isHint, ushort hint, string value)
    {
        var maxLength = Encoding.ASCII.GetMaxByteCount(value.Length);

        // Round it to the next even number
        if ((maxLength & 1) != 0)
        {
            maxLength++;
        }
        
        if (maxLength > 256)
        {
            var array = ArrayPool<byte>.Shared.Rent(maxLength + (isHint ? 2 : 0));
            try
            {
                WriteString(stream, array, isHint, hint, value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
        else
        {
            Span<byte> buffer = stackalloc byte[258];
            WriteString(stream, buffer, isHint, hint, value);
        }

        static void WriteString(Stream stream, Span<byte> buffer, bool isHint, ushort hint, string value)
        {
            var text = buffer;

            if (isHint)
            {
                MemoryMarshal.Write(buffer, hint);
                text = buffer.Slice(2);
            }

            int actualLength = Encoding.ASCII.GetBytes(value, text);
            text[actualLength] = 0;
            if ((actualLength & 1) != 0)
            {
                actualLength++;
            }

            if (isHint)
            {
                actualLength += 2;
            }

            stream.Write(buffer.Slice(0, actualLength));
        }
    }

    private static string ReadAsciiStringInternal(Stream stream, bool isHint, out ushort hint)
    {
        hint = 0;
        Span<byte> buffer = stackalloc byte[256];
        byte[]? charBuffer = null;
        Span<byte> currentString = default;

        if (isHint)
        {
            int read = stream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref hint, 1)));
            if (read != 2)
            {
                throw new EndOfStreamException();
            }
        }

        try
        {
            while (true)
            {
                int read = stream.Read(buffer);
                if (read == 0)
                {
                    break;
                }

                var indexOfZero = buffer.IndexOf((byte)0);
                var length = indexOfZero >= 0 ? indexOfZero : read;

                var sliceRead = buffer.Slice(0, length);
                if (charBuffer is null)
                {
                    return Encoding.ASCII.GetString(sliceRead);
                }

                var byteCountRequired = Encoding.ASCII.GetCharCount(sliceRead) * 2;
                if (byteCountRequired > charBuffer.Length - currentString.Length)
                {
                    var minimumLength = Math.Min(byteCountRequired + charBuffer.Length, charBuffer.Length);
                    var newCharBuffer = ArrayPool<byte>.Shared.Rent(minimumLength);
                    currentString.CopyTo(newCharBuffer);
                    ArrayPool<byte>.Shared.Return(charBuffer);
                    charBuffer = newCharBuffer;
                }

                var previousLength = currentString.Length;
                currentString = charBuffer.AsSpan(0, previousLength + byteCountRequired);
                Encoding.ASCII.GetChars(sliceRead, MemoryMarshal.Cast<byte, char>(currentString.Slice(previousLength)));
            }

            if (charBuffer is null)
            {
                throw new EndOfStreamException();
            }

            return Encoding.ASCII.GetString(currentString);
        }
        finally
        {
            if (charBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(charBuffer);
            }
        }
    }

}