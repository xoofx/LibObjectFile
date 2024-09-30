// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for all Portable Executable (PE) objects.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class PEObjectBase : ObjectFileElement<PELayoutContext, PEVisitorContext, PEImageReader, PEImageWriter>
{
    /// <summary>
    /// Gets the PE file containing this object.
    /// </summary>
    /// <returns>The PE file containing this object.</returns>
    /// <remarks>
    /// This method can return null if the object is not attached to a PE file.
    /// </remarks>
    public PEFile? GetPEFile() => FindParent<PEFile>();

    /// <summary>
    /// Gets a value indicating whether this object has children.
    /// </summary>
    public abstract bool HasChildren { get; }

    public virtual int ReadAt(uint offset, Span<byte> destination)
    {
        throw new NotSupportedException($"The read operation is not supported for {this.GetType().FullName}");
    }

    public virtual void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        throw new NotSupportedException($"The write operation is not supported for {this.GetType().FullName}");
    }


    public bool TryFindByPosition(uint position, [NotNullWhen(true)] out PEObjectBase? result)
    {
        if (Contains(position))
        {
            if (HasChildren && TryFindByPositionInChildren(position, out result))
            {
                return true;
            }

            result = this;
            return true;
        }

        result = null;
        return false;
    }

    protected virtual bool TryFindByPositionInChildren(uint position, [NotNullWhen(true)] out PEObjectBase? result)
    {
        throw new NotImplementedException("This method must be implemented by PEVirtualObject with children");
    }


    /// <summary>
    /// Gets the required alignment for this object.
    /// </summary>
    /// <param name="file">The PE file containing this object.</param>
    /// <returns>The required alignment for this object.</returns>
    /// <remarks>By default, this method returns 1.</remarks>
    public virtual uint GetRequiredPositionAlignment(PEFile file) => 1;
    
    /// <summary>
    /// Gets the required size alignment for this object.
    /// </summary>
    /// <param name="file">The PE file containing this object.</param>
    /// <returns>The required size alignment for this object.</returns>
    /// <remarks>By default, this method returns 1.</remarks>
    public virtual uint GetRequiredSizeAlignment(PEFile file) => 1;
}