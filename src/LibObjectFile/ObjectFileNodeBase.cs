// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LibObjectFile;

public abstract class ObjectFileNodeBase
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ObjectFileNodeBase? _parent;

    /// <summary>
    /// Gets or sets the position of this element relative to the top level parent.
    /// </summary>
    public ulong Position { get; set; }

    /// <summary>
    /// Gets the containing parent.
    /// </summary>
    public ObjectFileNodeBase? Parent
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

    protected virtual void ValidateParent(ObjectFileNodeBase parent)
    {
    }

    /// <summary>
    /// Index within the containing list in a parent.
    /// </summary>
    public uint Index { get; internal set; }

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
    /// <param name="node">The specified section or segment.</param>
    /// <returns><c>true</c> if the either the offset or end of the part is within this segment or section range.</returns>
    public bool Contains(ObjectFileNodeBase node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return Contains((ulong)node.Position) || node.Size != 0 && Contains((ulong)(node.Position + node.Size - 1));
    }

    /// <summary>
    /// Verifies the integrity of this file.
    /// </summary>
    /// <returns>The result of the diagnostics</returns>
    public DiagnosticBag Verify()
    {
        var diagnostics = new DiagnosticBag();
        Verify(diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Verifies the integrity of this file.
    /// </summary>
    /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
    public virtual void Verify(DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
    }

    protected static void AssignChild<TParent, T>(TParent parent, T child, out T field) where T : ObjectFileNodeBase where TParent : ObjectFileNodeBase
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
    
    protected static void AttachChild<TParent, T>(TParent parent, T child, ref T field) where T : ObjectFileNodeBase where TParent : ObjectFileNodeBase
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

    protected static void AttachNullableChild<TParent, T>(TParent parent, T? child, ref T? field) where T : ObjectFileNodeBase where TParent : ObjectFileNodeBase
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