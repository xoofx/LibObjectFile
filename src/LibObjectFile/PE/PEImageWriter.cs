// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

public sealed class PEImageWriter : ObjectFileReaderWriter
{
    internal PEImageWriter(PEFile file, Stream stream, PEImageWriterOptions options) : base(file, stream)
    {
        Options = options;
    }

    public PEFile PEFile => (PEFile)base.File;

    public PEImageWriterOptions Options { get; }

    public override bool KeepOriginalStreamForSubStreams => false;
}