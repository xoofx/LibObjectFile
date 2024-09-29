// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.PE;

/// <summary>
/// Represents an abstract base class for a PE resource entry.
/// </summary>
public abstract class PEResourceEntry : PESectionData
{
    public sealed override bool HasChildren => false;
    
    internal abstract void Read(in ReaderContext context);

    internal abstract void Write(in WriterContext context);

    internal readonly record struct ReaderContext(PEImageReader Reader, PEResourceDirectory Directory, List<PEResourceString> Strings, List<PEResourceEntry> Entries);

    internal readonly record struct WriterContext(PEImageWriter Writer, PEResourceDirectory Directory);
}
