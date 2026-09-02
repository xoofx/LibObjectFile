// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The build version load command (<c>LC_BUILD_VERSION</c>).
/// </summary>
/// <remarks>
/// This replaced the <c>LC_VERSION_MIN_*</c> family, carrying the platform as a field rather
/// than encoding it in the command type, and adding the list of tools that produced the image.
/// </remarks>
public sealed class MachOBuildVersionCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of the command excluding its tool entries.
    /// </summary>
    public const uint HeaderSize = 24;

    /// <summary>
    /// The size of one tool entry.
    /// </summary>
    public const uint ToolSize = 8;

    /// <summary>
    /// Gets or sets the platform the image targets.
    /// </summary>
    public MachOPlatform Platform { get; set; }

    /// <summary>
    /// Gets or sets the packed minimum OS version.
    /// </summary>
    public uint MinOSVersion { get; set; }

    /// <summary>
    /// Gets or sets the packed SDK version the image was built against.
    /// </summary>
    public uint SdkVersion { get; set; }

    /// <summary>
    /// Gets the tools that produced this image.
    /// </summary>
    public List<MachOBuildToolVersion> Tools { get; } = [];

    /// <summary>
    /// Gets the minimum OS version.
    /// </summary>
    public Version MinOS => MachOVersion.Decode(MinOSVersion);

    /// <summary>
    /// Gets the SDK version.
    /// </summary>
    public Version Sdk => MachOVersion.Decode(SdkVersion);

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context)
        => Size = HeaderSize + (uint)Tools.Count * ToolSize;

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawBuildVersionCommand), out RawBuildVersionCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_BUILD_VERSION at 0x{Position:X}");
            return;
        }

        Platform = (MachOPlatform)raw.Platform;
        MinOSVersion = raw.MinOS;
        SdkVersion = raw.Sdk;

        Tools.Clear();
        for (uint i = 0; i < raw.ToolCount; i++)
        {
            if (!reader.TryReadData(sizeof(RawBuildToolVersion), out RawBuildToolVersion tool))
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated tool entry in LC_BUILD_VERSION at 0x{Position:X}");
                return;
            }

            Tools.Add(new MachOBuildToolVersion((MachOBuildTool)tool.Tool, tool.Version));
        }
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.Write(new RawBuildVersionCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            Platform = (uint)Platform,
            MinOS = MinOSVersion,
            Sdk = SdkVersion,
            ToolCount = (uint)Tools.Count,
        });

        foreach (var tool in Tools)
        {
            writer.Write(new RawBuildToolVersion { Tool = (uint)tool.Tool, Version = tool.PackedVersion });
        }
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Platform = {Platform}, MinOS = {MinOS}, Sdk = {Sdk}, Tools = {Tools.Count}");
        return true;
    }
}
