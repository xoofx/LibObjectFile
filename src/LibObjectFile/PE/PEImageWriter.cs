// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

public sealed class PEImageWriter : ObjectFileReaderWriter
{
    internal PEImageWriter(PEFile file, Stream stream) : base(file, stream)
    {
    }

    public PEFile PEFile => (PEFile)base.File;

    public override bool IsReadOnly => false;
}