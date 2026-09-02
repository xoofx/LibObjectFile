// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The Mach-O header, which is always the first content of an image.
/// </summary>
/// <remarks>
/// The header is generated from the file it belongs to rather than held as bytes, so the counts
/// it records cannot drift from the load commands actually present.
/// </remarks>
public sealed class MachOHeaderContent : MachOContent
{
    /// <inheritdoc />
    public override bool IsPositionPinned => true;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = context.File.HeaderSize;

    /// <inheritdoc />
    public override unsafe void WriteContent(MachOWriter writer)
    {
        var file = writer.File;

        if (file.Is64Bit)
        {
            writer.Write(new RawMachHeader64
            {
                Magic = MachOMagic.Magic64,
                CpuType = (uint)file.CpuType,
                CpuSubType = file.CpuSubType,
                FileType = (uint)file.FileType,
                NumberOfCommands = (uint)file.LoadCommands.Count,
                SizeOfCommands = file.SizeOfCommands,
                Flags = (uint)file.Flags,
                Reserved = file.Reserved,
            });
        }
        else
        {
            writer.Write(new RawMachHeader32
            {
                Magic = MachOMagic.Magic32,
                CpuType = (uint)file.CpuType,
                CpuSubType = file.CpuSubType,
                FileType = (uint)file.FileType,
                NumberOfCommands = (uint)file.LoadCommands.Count,
                SizeOfCommands = file.SizeOfCommands,
                Flags = (uint)file.Flags,
            });
        }
    }
}
