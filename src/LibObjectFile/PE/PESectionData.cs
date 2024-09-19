// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for data contained in a <see cref="PESection"/>.
/// </summary>
public abstract class PESectionData : PEVirtualObject
{
    protected PESectionData(bool hasChildren) : base(hasChildren)
    {
    }

    public virtual int ReadAt(uint offset, Span<byte> destination)
    {
        throw new NotSupportedException($"The read operation is not supported for {this.GetType().FullName}");
    }

    public virtual void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        throw new NotSupportedException($"The write operation is not supported for {this.GetType().FullName}");
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"VirtualAddress =  {VirtualAddress}, Size = 0x{Size:X4}");
        if (Parent is PESection section)
        {
            builder.Append($", Section = {section.Name}");
        }
        return true;
    }

    protected override void ValidateParent(ObjectFileElement parent)
    {
        if (parent is not PESection && parent is not PESectionData)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PESection).FullName} or {typeof(PESectionData).FullName}");
        }
    }
}