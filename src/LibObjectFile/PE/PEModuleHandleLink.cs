// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A link to a module handle.
/// </summary>
public readonly struct PEModuleHandleLink : IRVALink
{
    public PEModuleHandleLink(PEStreamSectionData? streamSectionData, RVO rvo)
    {
        StreamSectionData = streamSectionData;
        RVO = rvo;
    }

    public readonly PEStreamSectionData? StreamSectionData;

    public PEObject? Container => StreamSectionData;

    public RVO RVO { get; }

    /// <inheritdoc />
    public override string ToString() => this.ToDisplayText();
}