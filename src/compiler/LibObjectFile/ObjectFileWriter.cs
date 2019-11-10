using System;
using System.IO;

namespace LibObjectFile
{
    public abstract class ObjectFileWriter
    {
        protected ObjectFileWriter(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Diagnostics = new DiagnosticBag();
        }

        public Stream Stream { get; }

        public DiagnosticBag Diagnostics { get; }

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