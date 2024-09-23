// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A link to a null terminated ASCII string in a <see cref="PESectionData"/>.
/// </summary>
public readonly struct PEAsciiStringLink : IRVALink<string?>
{
    public PEAsciiStringLink(PEStreamSectionData? streamSectionData, RVO rvo)
    {
        StreamSectionData = streamSectionData;
        RVO = rvo;
    }

    public readonly PEStreamSectionData? StreamSectionData;

    public PEObject? Container => StreamSectionData;

    public RVO RVO { get; }

    /// <inheritdoc />
    public override string ToString() => this.ToDisplayText();

    /// <summary>
    /// Resolves this link to a string.
    /// </summary>
    /// <returns>The string resolved.</returns>
    public string? Resolve() => StreamSectionData?.ReadAsciiString(RVO);
}