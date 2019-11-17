// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile
{
    /// <summary>
    /// Base class used for reading / writing an object file to/from a stream.
    /// </summary>
    public abstract class ObjectFileReaderWriter
    {
        protected ObjectFileReaderWriter(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Diagnostics = new DiagnosticBag();
        }

        /// <summary>
        /// The stream of the object file.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// The diagnostics while read/writing this object file.
        /// </summary>
        public DiagnosticBag Diagnostics { get; }

        /// <summary>
        /// Gets a boolean indicating if this reader is operating in read-only mode.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Reads from the <see cref="Stream"/> and current position to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to receive the content of the read.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to write from the buffer.</param>
        public int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads a null terminated UTF8 string from the stream.
        /// </summary>
        /// <param name="byteLength">The number of bytes to read including the null</param>
        /// <returns>A string</returns>
        public string ReadStringUTF8NullTerminated(uint byteLength)
        {
            if (byteLength == 0) return string.Empty;

            var buffer = ArrayPool<byte>.Shared.Rent((int)byteLength);
            try
            {
                var dataLength = Stream.Read(buffer, 0, (int) byteLength);
                if (dataLength < 0) throw new EndOfStreamException("Unexpected end of stream while trying to read data");

                var byteReadLength = (uint) dataLength;
                if (byteReadLength != byteLength) throw new EndOfStreamException($"Not enough data read {byteReadLength} bytes while expecting to read {byteLength} bytes");

                var isNullTerminated = buffer[byteReadLength - 1] == 0;

                var text = Encoding.UTF8.GetString(buffer, 0, (int) (isNullTerminated ? byteReadLength - 1 : byteReadLength));
                return text;
            } 
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Writes a null terminated UTF8 string to the stream.
        /// </summary>
        public void WriteStringUTF8NullTerminated(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            
            int byteLength = Encoding.UTF8.GetByteCount(text);
            var buffer = ArrayPool<byte>.Shared.Rent(byteLength + 1);
            try
            {
                Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
                buffer[byteLength] = 0;
                Stream.Write(buffer, 0, byteLength + 1);
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
        public unsafe bool TryRead<T>(int sizeToRead, out T data) where T : unmanaged
        {
            if (sizeToRead <= 0) throw new ArgumentOutOfRangeException(nameof(sizeToRead));

            int dataByteCount = sizeof(T);
            int byteRead;

            // If we are requested to read more data than the sizeof(T)
            // we need to read it to an intermediate buffer before transferring it to T data
            if (sizeToRead > dataByteCount)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(sizeToRead);
                var span = new Span<byte>(buffer);
                byteRead = Stream.Read(span);
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

                fixed (void* pData = &data)
                {
                    var span = new Span<byte>(pData, sizeToRead);
                    byteRead = Stream.Read(span);
                }
            }
            return byteRead == sizeToRead;
        }

        /// <summary>
        /// Reads from the current <see cref="Stream"/> <see cref="size"/> bytes and return the data as
        /// a <see cref="SliceStream"/> if <see cref="IsReadOnly"/> is <c>false</c> otherwise as a 
        /// <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="size">Size of the data to read.</param>
        /// <returns>A <see cref="SliceStream"/> if <see cref="IsReadOnly"/> is <c>false</c> otherwise as a 
        /// <see cref="MemoryStream"/>.</returns>
        public Stream ReadAsStream(ulong size)
        {
            if (IsReadOnly)
            {
                return ReadAsSliceStream(size);
            }

            return ReadAsMemoryStream(size);
        }

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
        /// </summary>
        /// <param name="inputStream">The input stream to read from and write to <see cref="Stream"/></param>
        /// <param name="size">The amount of data to read from the input stream (if == 0, by default, it will read the entire input stream)</param>
        /// <param name="bufferSize">The size of the intermediate buffer used to transfer the data.</param>
        public void Write(Stream inputStream, ulong size = 0, int bufferSize = 4096)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            size = size == 0 ? (ulong)inputStream.Length - (ulong)inputStream.Position : size;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            while (size != 0)
            {
                var sizeToRead = size >= (ulong)buffer.Length ? buffer.Length : (int)size;
                var sizeRead = inputStream.Read(buffer, 0, sizeToRead);
                if (sizeRead <= 0) break;

                Stream.Write(buffer, 0, sizeRead);
                size -= (ulong)sizeRead;
            }

            if (size != 0)
            {
                throw new InvalidOperationException("Unable to write stream entirely");
            }
        }
        
        private SliceStream ReadAsSliceStream(ulong size)
        {
            return new SliceStream(Stream, Stream.Position, (long)size);
        }

        private MemoryStream ReadAsMemoryStream(ulong size)
        {
            var memoryStream = new MemoryStream((int)size);
            if (size == 0) return memoryStream;

            memoryStream.SetLength((long)size);

            var buffer = memoryStream.GetBuffer();
            while (size != 0)
            {
                var lengthToRead = size >= int.MaxValue ? int.MaxValue : (int)size;
                var lengthRead = Stream.Read(buffer, 0, lengthToRead);
                if (lengthRead < 0) break;
                if ((uint)lengthRead >= size)
                {
                    size -= (uint)lengthRead;
                }
                else
                {
                    break;
                }
            }
            return memoryStream;
        }
    }
}