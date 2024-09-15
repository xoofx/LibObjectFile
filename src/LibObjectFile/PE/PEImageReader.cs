// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

public sealed class PEImageReader : ObjectFileReaderWriter
{
    internal PEImageReader(PEFile file, Stream stream, PEImageReaderOptions readerOptions) : base(stream)
    {
        PEFile = file;
        Options = readerOptions;
    }

    public PEFile PEFile { get; }

    public PEImageReaderOptions Options { get; }

    public override bool IsReadOnly => Options.IsReadOnly;
}