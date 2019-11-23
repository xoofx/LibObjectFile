// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Equivalent of <see cref="ElfCustomSection"/> but used for shadow.
    /// </summary>
    public sealed class ElfCustomShadowSection : ElfShadowSection
    {
        public ElfCustomShadowSection()
        {
        }

        public Stream Stream { get; set; }

        protected override ulong GetSizeAuto() => Stream != null ? (ulong)Stream.Length : 0;

        protected override void Read(ElfReader reader)
        {
            Stream = reader.ReadAsStream(Size);
            SizeKind = ValueKind.Manual;
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            writer.Write(Stream);
        }
    }
}