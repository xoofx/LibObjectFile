// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile;

public abstract class ObjectFileElement : ObjectElement
{

    protected ObjectFileElement()
    {
    }

    /// <summary>
    /// Gets or sets the absolute position of this element relative to the top level parent.
    /// </summary>
    public ulong Position { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes of this element in the top level parent. This value might need to be updated via UpdateLayout on the top level parent.
    /// </summary>
    public ulong Size { get; set; }

    /// <summary>
    /// Checks if the specified offset is contained by this instance.
    /// </summary>
    /// <param name="position">The offset to check if it belongs to this instance.</param>
    /// <returns><c>true</c> if the offset is within the segment or section range.</returns>
    public bool Contains(ulong position)
    {
        return position >= Position && position < Position + Size;
    }

    public bool Contains(ulong position, uint size)
    {
        return position >= Position && position + size <= Position + Size;
    }

    /// <summary>
    /// Checks this instance contains either the beginning or the end of the specified section or segment.
    /// </summary>
    /// <param name="element">The specified section or segment.</param>
    /// <returns><c>true</c> if either the offset or end of the part is within this segment or section range.</returns>
    public bool Contains(ObjectFileElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Contains((ulong)element.Position) || element.Size != 0 && Contains((ulong)(element.Position + element.Size - 1));
    }
   
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Position = 0x{Position:X}, Size = 0x{Size:X}");
        return true;
    }

    /// <summary>
    /// Finds the parent of the specified type.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent to find.</typeparam>
    /// <returns>The parent of the specified type or null if not found.</returns>
    public TParent? FindParent<TParent>() where TParent : ObjectElement
    {
        ObjectElement? thisObject = this;
        while (thisObject is not null)
        {
            if (thisObject is TParent parent)
            {
                return parent;
            }
            thisObject = thisObject.Parent;
        }

        return null;
    }
}

public abstract class ObjectFileElement<TLayoutContext, TVerifyContext, TReader, TWriter> : ObjectFileElement
    where TLayoutContext : VisitorContextBase
    where TVerifyContext : VisitorContextBase
    where TReader : ObjectFileReaderWriter
    where TWriter : ObjectFileReaderWriter
{
    public virtual void UpdateLayout(TLayoutContext layoutContext)
    {
    }

    public virtual void Verify(TVerifyContext diagnostics)
    {
    }

    public virtual void Read(TReader reader)
    {
    }

    public virtual void Write(TWriter writer)
    {
    }
}