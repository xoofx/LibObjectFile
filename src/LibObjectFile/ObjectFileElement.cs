// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile;

public abstract class ObjectFileElement
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ObjectFileElement? _parent;

    protected ObjectFileElement()
    {
        Index = -1;
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
    /// Gets the containing parent.
    /// </summary>
    public ObjectFileElement? Parent
    {
        get => _parent;

        internal set
        {
            if (value == null)
            {
                _parent = null;
            }
            else
            {
                ValidateParent(value);
            }

            _parent = value;
        }
    }

    /// <summary>
    /// If the object is part of a list in its parent, this property returns the index within the containing list in the parent. Otherwise, this value is -1.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    /// Checks if the specified offset is contained by this instance.
    /// </summary>
    /// <param name="offset">The offset to check if it belongs to this instance.</param>
    /// <returns><c>true</c> if the offset is within the segment or section range.</returns>
    public bool Contains(ulong offset)
    {
        return offset >= Position && offset < Position + Size;
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
    
    public sealed override string ToString()
    {
        var builder = new StringBuilder();
        PrintName(builder);
        builder.Append(" { ");
        if (PrintMembers(builder))
        {
            builder.Append(' ');
        }
        builder.Append('}');
        return builder.ToString();
    }

    protected virtual void PrintName(StringBuilder builder)
    {
        builder.Append(GetType().Name);
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return false;
    }

    protected virtual void ValidateParent(ObjectFileElement parent)
    {
    }

    internal void ResetIndex()
    {
        Index = -1;
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