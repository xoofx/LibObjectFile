// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// Internal implementation of <see cref="ElfWriter{TEncoder}"/> with a <see cref="ElfDecoderDirect"/>.
/// </summary>
internal sealed class ElfWriterDirect : ElfWriter<ElfEncoderDirect>
{
    public ElfWriterDirect(ElfFile elfFile, Stream stream) : base(elfFile, stream)
    {
    }
}