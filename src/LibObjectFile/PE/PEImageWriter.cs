// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

public sealed class PEImageWriter : ObjectFileReaderWriter
{
    internal PEImageWriter(PEFile file, Stream stream) : base(stream)
    {
        PEFile = file;
    }

    public PEFile PEFile { get; }

    public override bool IsReadOnly => false;
}