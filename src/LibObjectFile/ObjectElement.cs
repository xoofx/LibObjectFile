// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile;

public abstract class ObjectElement
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ObjectElement? _parent;

    protected ObjectElement()
    {
        Index = -1;
    }

    /// <summary>
    /// Gets the containing parent.
    /// </summary>
    public ObjectElement? Parent
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

    protected virtual void ValidateParent(ObjectElement parent)
    {
    }

    internal void ResetIndex()
    {
        Index = -1;
    }
}