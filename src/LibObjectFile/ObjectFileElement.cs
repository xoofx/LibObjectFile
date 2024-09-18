// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using System;
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
    /// Gets or sets the position of this element relative to the top level parent.
    /// </summary>
    public ulong Position { get; set; }

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

    protected virtual void ValidateParent(ObjectFileElement parent)
    {
    }

    /// <summary>
    /// Index within the containing list in a parent. If this object is not part of a list, this value is -1.
    /// </summary>
    public int Index { get; internal set; }

    internal void ResetIndex()
    {
        Index = -1;
    }

    /// <summary>
    /// Gets or sets the size of this section or segment in the parent <see cref="TParentFile"/>.
    /// </summary>
    public virtual ulong Size { get; set; }

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
    /// <returns><c>true</c> if the either the offset or end of the part is within this segment or section range.</returns>
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

    protected static void AssignChild<TParent, T>(TParent parent, T child, out T field) where T : ObjectFileElement where TParent : ObjectFileElement
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (child == null) throw new ArgumentNullException(nameof(child));

        if (child?.Parent != null) throw new InvalidOperationException($"Cannot set the {child.GetType()} as it already belongs to another {child.Parent.GetType()} instance");
        field = child!;

        if (child != null)
        {
            child.Parent = parent;
        }
    }

    protected static void AttachChild<TParent, T>(TParent parent, T child, ref T field) where T : ObjectFileElement where TParent : ObjectFileElement
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        field.Parent = null;

        if (child?.Parent != null) throw new InvalidOperationException($"Cannot set the {child.GetType()} as it already belongs to another {child.Parent.GetType()} instance");
        field = child!;

        if (child != null)
        {
            child.Parent = parent;
        }
    }

    protected static void AttachNullableChild<TParent, T>(TParent parent, T? child, ref T? field) where T : ObjectFileElement where TParent : ObjectFileElement
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));

        if (field is not null)
        {
            field.Parent = null;
        }

        if (child?.Parent != null) throw new InvalidOperationException($"Cannot set the {child.GetType()} as it already belongs to another {child.Parent.GetType()} instance");
        field = child!;

        if (child != null)
        {
            child.Parent = parent;
        }
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
