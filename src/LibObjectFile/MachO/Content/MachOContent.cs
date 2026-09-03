// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// A run of bytes in a Mach-O image, holding its place in the file.
/// </summary>
/// <remarks>
/// Everything in an image is one of these, in one ordered list: the header, the load command
/// table, the padding after it, each section's bytes, each table in <c>__LINKEDIT</c> and any
/// alignment padding between them. Nothing is left implicit, so writing the list back out
/// reproduces the file and a layout is a single walk over it.
/// <para>
/// <see cref="IsPositionPinned"/> is where Mach-O parts company with ELF. A section's address is
/// its segment's address plus its distance from the segment's file offset, so moving a section
/// in the file moves it in memory and invalidates everything referring to it. Content carrying
/// an address is therefore pinned and a layout leaves it alone; only what nothing addresses,
/// which in practice is <c>__LINKEDIT</c> and the file tail, is free to move.
/// </para>
/// </remarks>
public abstract class MachOContent : MachOObject
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    protected MachOContent()
    {
        FileAlignment = 1;
    }

    /// <summary>
    /// Gets or sets the alignment this content requires in the file.
    /// </summary>
    public uint FileAlignment { get; set; }

    /// <summary>
    /// Gets whether a layout has to leave this content where it is, because something records
    /// its address and moving it in the file would move it in memory.
    /// </summary>
    public virtual bool IsPositionPinned => false;

    /// <summary>
    /// Writes the bytes of this content. The writer is positioned at <see cref="ObjectFileElement.Position"/>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    public abstract void WriteContent(MachOWriter writer);
}
