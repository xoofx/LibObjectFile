// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a null terminated ASCII string in a <see cref="PESectionData"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PEAsciiStringLink(PEStreamSectionData? Container, RVO RVO) : IPELink<PEStreamSectionData, string?>
{
    /// <inheritdoc />
    public override string ToString() => this.ToDisplayTextWithRVA();

    /// <summary>
    /// Resolves this link to a string.
    /// </summary>
    /// <returns>The string resolved.</returns>
    public string? Resolve() => Container?.ReadAsciiString(RVO);
}