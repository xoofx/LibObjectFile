// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// A load command that is kept as raw bytes rather than being decoded.
/// </summary>
/// <remarks>
/// This is the fallback for command types the library does not model, and it is what makes a
/// byte-exact round-trip possible for images using commands added after this code was written.
/// The payload excludes the <c>cmd</c> and <c>cmdsize</c> fields, which are rebuilt on write.
/// </remarks>
public sealed class MachOUnknownLoadCommand : MachOLoadCommand
{
    /// <summary>
    /// Gets or sets the raw payload following the <c>cmd</c> and <c>cmdsize</c> fields.
    /// </summary>
    public byte[] Payload { get; set; } = [];

    /// <inheritdoc />
    public override void Read(MachOReader reader)
    {
        reader.ReadU32();
        reader.ReadU32();
        Payload = new byte[Size - 8];
        reader.ReadExactly(Payload);
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.WriteU32((uint)Type);
        writer.WriteU32((uint)(8 + Payload.Length));
        writer.Write(Payload);
    }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = (ulong)(8 + Payload.Length);
}
