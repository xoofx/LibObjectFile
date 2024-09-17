// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibObjectFile.PE;

public static class PESectionDataExtensions
{
    public static string ReadZeroTerminatedAsciiString(this PESectionData sectionData, uint offset)
    {
        Span<byte> buffer = stackalloc byte[256];
        byte[]? charBuffer = null;
        Span<byte> currentString = default;
        try
        {
            while (true)
            {
                int read = sectionData.ReadAt(offset, buffer);
                if (read == 0)
                {
                    break;
                }

                var indexOfZero = buffer.IndexOf((byte)0);
                var length  = indexOfZero >= 0 ? indexOfZero : read;
                
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