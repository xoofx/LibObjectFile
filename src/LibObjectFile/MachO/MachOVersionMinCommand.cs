// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// A minimum OS version load command, covering <c>LC_VERSION_MIN_MACOSX</c>,
/// <c>LC_VERSION_MIN_IPHONEOS</c>, <c>LC_VERSION_MIN_TVOS</c> and <c>LC_VERSION_MIN_WATCHOS</c>.
/// </summary>
/// <remarks>
/// The platform is encoded in the command type, which is why there is one command per platform.
/// <see cref="MachOBuildVersionCommand"/> replaced the whole family with a single command
/// carrying the platform as a field.
/// </remarks>
public sealed class MachOVersionMinCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 16;

    /// <summary>
    /// Gets or sets the packed minimum OS version.
    /// </summary>
    public uint MinOSVersion { get; set; }

    /// <summary>
    /// Gets or sets the packed SDK version the image was built against.
    /// </summary>
    public uint SdkVersion { get; set; }

    /// <summary>
    /// Gets the minimum OS version.
    /// </summary>
    public Version MinOS => MachOVersion.Decode(MinOSVersion);

    /// <summary>
    /// Gets the SDK version.
    /// </summary>
    public Version Sdk => MachOVersion.Decode(SdkVersion);

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawVersionMinCommand), out RawVersionMinCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated {Type} at 0x{Position:X}");
            return;
        }

        MinOSVersion = raw.Version;
        SdkVersion = raw.Sdk;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
        => writer.Write(new RawVersionMinCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            Version = MinOSVersion,
            Sdk = SdkVersion,
        });

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, MinOS = {MinOS}, Sdk = {Sdk}");
        return true;
    }
}
