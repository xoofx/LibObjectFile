// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Elf;

public abstract class ElfObject : ObjectFileElement<ElfVisitorContext, ElfVisitorContext, ElfReader, ElfWriter>
{
}

/// <summary>
/// Base class for an <see cref="ElfSection"/> and <see cref="ElfSegment"/>.
/// </summary>
public abstract class ElfContent : ElfObject
{
    protected ElfContent()
    {
        FileAlignment = 1;
    }

    /// <summary>
    /// Gets or sets the alignment requirement of this content in the file.
    /// </summary>
    public uint FileAlignment { get; set; }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (!(parent is ElfFile))
        {
            throw new ArgumentException($"Parent must inherit from type {nameof(ElfFile)}");
        }
    }

    protected void ValidateParent(ObjectElement parent, ElfFileClass fileClass)
    {
        if (!(parent is ElfFile file))
        {
            throw new ArgumentException($"Parent must inherit from type {nameof(ElfFile)} with class {fileClass}");
        }

        if (file.FileClass != fileClass)
        {
            throw new ArgumentException($"Parent must be an ELF file with class {fileClass}");
        }
    }

    /// <summary>
    /// Gets the containing <see cref="ElfFile"/>. Might be null if this section or segment
    /// does not belong to an existing <see cref="ElfFile"/>.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new ElfFile? Parent
    {
        get => (ElfFile?)base.Parent;
        internal set => base.Parent = value;
    }
}
