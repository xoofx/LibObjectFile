// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// The load command table, which follows the header and describes everything else.
/// </summary>
/// <remarks>
/// The table is pinned because dyld expects it directly after the header, and because the
/// padding the linker leaves after it is what bounds how far it can grow.
/// </remarks>
public sealed class MachOLoadCommandTable : MachOContent
{
    /// <inheritdoc />
    public override bool IsPositionPinned => true;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context)
    {
        foreach (var command in context.File.LoadCommands)
        {
            command.UpdateLayout(context);
        }

        Size = context.File.SizeOfCommands;
    }

    /// <inheritdoc />
    public override void WriteContent(MachOWriter writer)
    {
        foreach (var command in writer.File.LoadCommands)
        {
            command.Write(writer);
        }
    }
}
