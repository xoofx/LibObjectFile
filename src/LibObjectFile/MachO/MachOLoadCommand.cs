// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Base class for a Mach-O load command.
/// </summary>
/// <remarks>
/// <see cref="ObjectFileElement.Size"/> is the command's <c>cmdsize</c>, which covers the
/// <c>cmd</c> and <c>cmdsize</c> fields themselves. It is always a multiple of 4 on 32-bit
/// images and of 8 on 64-bit ones, because dyld walks the list by adding it to a pointer.
/// </remarks>
public abstract class MachOLoadCommand : MachOObject
{
    /// <summary>
    /// Gets or sets the type of this load command.
    /// </summary>
    public MachOLoadCommandType Type { get; set; }

    /// <summary>
    /// Rewrites every file offset this command records, so a layout can move the data they point
    /// at without each caller having to know which commands carry offsets.
    /// </summary>
    /// <param name="mapper">Maps an old file offset to its new one.</param>
    /// <remarks>
    /// An offset of zero means the data is absent rather than located at the start of the file,
    /// so it is left alone and <paramref name="mapper"/> is not called for it.
    /// </remarks>
    public virtual void UpdateFileOffsets(Func<uint, uint> mapper)
    {
    }

    /// <summary>
    /// Gets the alignment <c>cmdsize</c> has to satisfy for the given image width.
    /// </summary>
    /// <param name="is64Bit">Whether the containing image is 64-bit.</param>
    /// <returns>4 for a 32-bit image, 8 for a 64-bit one.</returns>
    public static uint GetSizeAlignment(bool is64Bit) => is64Bit ? 8u : 4u;

    /// <inheritdoc />
    protected override bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Type = {Type}, Size = {Size}");
        return true;
    }
}
