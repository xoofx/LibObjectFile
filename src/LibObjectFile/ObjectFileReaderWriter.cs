using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace LibObjectFile
{
    public abstract class ObjectFileReaderWriter
    {
        protected ObjectFileReaderWriter(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Diagnostics = new DiagnosticBag();
        }

        public Stream Stream { get; }

        public DiagnosticBag Diagnostics { get; }

        public void Read(byte[] buffer, int offset, int count)
        {
            Stream.Read(buffer, offset, count);
        }

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

        public void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        public unsafe void Write<T>(in T data) where T : unmanaged
        {
            fixed (void* pData = &data)
            {
                var span = new ReadOnlySpan<byte>(pData, sizeof(T));
                Stream.Write(span);
            }
        }
    }
}