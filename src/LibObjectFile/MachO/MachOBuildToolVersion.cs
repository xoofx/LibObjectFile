// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// A tool that took part in producing an image, as recorded by <c>LC_BUILD_VERSION</c>.
/// </summary>
public enum MachOBuildTool : uint
{
    /// <summary>The tool is not one of the known values.</summary>
    Unknown = 0,
    /// <summary>The Clang compiler (<c>TOOL_CLANG</c>).</summary>
    Clang = 1,
    /// <summary>The Swift compiler (<c>TOOL_SWIFT</c>).</summary>
    Swift = 2,
    /// <summary>The static linker (<c>TOOL_LD</c>).</summary>
    Ld = 3,
    /// <summary>An alternative linker (<c>TOOL_LLD</c>).</summary>
    Lld = 4,
}

/// <summary>
/// One entry of the tool list in <see cref="MachOBuildVersionCommand"/>.
/// </summary>
/// <param name="Tool">The tool.</param>
/// <param name="PackedVersion">The tool's version, packed as 16.8.8 bits.</param>
public readonly record struct MachOBuildToolVersion(MachOBuildTool Tool, uint PackedVersion)
{
    /// <summary>
    /// Gets the tool's version.
    /// </summary>
    public Version Version => MachOVersion.Decode(PackedVersion);

    /// <inheritdoc />
    public override string ToString() => $"{Tool} {Version}";
}
