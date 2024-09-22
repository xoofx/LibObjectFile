// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

public sealed class PEImageReader : ObjectFileReaderWriter
{
    internal PEImageReader(PEFile file, Stream stream, PEImageReaderOptions readerOptions) : base(file, stream)
    {
        Options = readerOptions;
        LayoutContext = new PELayoutContext(File, Diagnostics, true);
    }

    public new PEFile File => (PEFile)base.File;

    public PEImageReaderOptions Options { get; }

    public override bool KeepOriginalStreamForSubStreams => Options.IsReadOnly;

    public PELayoutContext LayoutContext { get; }


    public static implicit operator PELayoutContext(PEImageReader reader) => reader.LayoutContext;
}