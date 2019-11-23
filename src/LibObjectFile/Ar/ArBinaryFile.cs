// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// An binary stream <see cref="ArFile"/>.
    /// </summary>
    public sealed class ArBinaryFile : ArFile
    {
        /// <summary>
        /// Gets or sets the stream associated to this entry.
        /// </summary>
        public Stream Stream { get; set; }

        protected override ulong GetSizeAuto()
        {
            return Stream != null ? (ulong)Stream.Length : 0;
        }
        
        protected override void Read(ArArchiveFileReader reader)
        {
            Stream = reader.ReadAsStream(Size);
            SizeKind = ValueKind.Auto;
        }

        protected override void Write(ArArchiveFileWriter writer)
        {
            if (Stream != null)
            {
                writer.Write(Stream);
            }
        }
    }
}