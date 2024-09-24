﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

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
    
    public virtual int ReadAt(uint offset, Span<byte> destination)
    {
        throw new NotSupportedException($"The read operation is not supported for {this.GetType().FullName}");
    }

    public virtual void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        throw new NotSupportedException($"The write operation is not supported for {this.GetType().FullName}");
    }
}