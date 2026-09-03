// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.MachO;

/// <summary>
/// Content held as a stream of bytes this library does not interpret.
/// </summary>
/// <remarks>
/// This covers alignment padding as well as anything else the file contains. Padding is kept as
/// the bytes that were there rather than regenerated: a linker pads executable sections with
/// <c>nop</c> rather than zeros, so filling a gap with zeros would put a different instruction
/// in a place that can be reached.
/// </remarks>
public class MachOStreamContent : MachOContent
{
    private Stream _content;

    /// <summary>
    /// Initializes a new instance holding the given bytes.
    /// </summary>
    /// <param name="content">The bytes of this content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    public MachOStreamContent(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
        Size = (ulong)content.Length;
    }

    /// <summary>
    /// Gets or sets the bytes of this content.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is null.</exception>
    public Stream Content
    {
        get => _content;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _content = value;
            Size = (ulong)value.Length;
        }
    }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = (ulong)_content.Length;

    /// <inheritdoc />
    public override void WriteContent(MachOWriter writer)
    {
        _content.Position = 0;
        writer.Write(_content, (ulong)_content.Length);
    }

    /// <inheritdoc />
    protected override bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Position = 0x{Position:X}, Size = 0x{Size:X}");
        return true;
    }
}
