// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.MachO;

/// <summary>
/// The padding a linker leaves between the load command table and whatever follows it.
/// </summary>
/// <remarks>
/// Adding a load command consumes this from the front, since the table grows into it, so what
/// survives is its tail. That is what bounds an <c>install_name_tool</c>-style edit: once the
/// padding is used up there is nowhere for another command to go without moving content, and
/// moving content in a Mach-O moves addresses with it.
/// </remarks>
public sealed class MachOLoadCommandPadding : MachOStreamContent
{
    /// <summary>
    /// Initializes a new instance holding the padding bytes.
    /// </summary>
    /// <param name="content">The padding bytes as the linker left them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    public MachOLoadCommandPadding(Stream content) : base(content)
    {
    }

    /// <inheritdoc />
    public override bool IsPositionPinned => true;

    /// <summary>
    /// Keeps the size the layout assigned rather than taking it from the bytes held, because the
    /// load command table may have grown into them since they were read.
    /// </summary>
    protected override void UpdateLayoutCore(MachOVisitorContext context)
    {
    }

    /// <inheritdoc />
    public override void WriteContent(MachOWriter writer)
    {
        // The commands grew into the front of this, so the part that survives is the tail.
        var available = (long)Content.Length;
        var keep = (long)Size;

        if (keep > available)
        {
            writer.WriteZero((int)(keep - available));
            keep = available;
        }

        Content.Position = available - keep;
        writer.Write(Content, (ulong)keep);
    }
}
