// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.MachO;

/// <summary>
/// The bytes of a <see cref="MachOSection"/>.
/// </summary>
/// <remarks>
/// This is pinned. A section's address is its segment's address plus its distance from the
/// segment's file offset, so moving it in the file moves it in memory, and every instruction and
/// relocation referring to it would then be wrong. Only relinking can move a section.
/// </remarks>
public sealed class MachOSectionData : MachOStreamContent
{
    /// <summary>
    /// Initializes a new instance holding the bytes of a section.
    /// </summary>
    /// <param name="section">The section these bytes belong to.</param>
    /// <param name="content">The bytes of the section.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> or <paramref name="content"/> is null.</exception>
    public MachOSectionData(MachOSection section, Stream content) : base(content)
    {
        ArgumentNullException.ThrowIfNull(section);
        Section = section;
    }

    /// <summary>
    /// Gets the section these bytes belong to.
    /// </summary>
    public MachOSection Section { get; }

    /// <inheritdoc />
    public override bool IsPositionPinned => true;

    /// <inheritdoc />
    protected override bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"{Section.SegmentName},{Section.Name} Position = 0x{Position:X}, Size = 0x{Size:X}");
        return true;
    }
}
