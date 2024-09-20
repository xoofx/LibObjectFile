// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A link to a null terminated ASCII string in a <see cref="PESectionData"/>.
/// </summary>
/// <param name="Link">The link.</param>
public readonly record struct PEAsciiStringLink(RVALink<PEStreamSectionData> Link)
{
    /// <summary>
    /// Gets a value indicating whether this instance is null.
    /// </summary>
    public bool IsNull => Link.IsNull;

    /// <summary>
    /// Resolves this link to a string.
    /// </summary>
    /// <returns>The string resolved.</returns>
    public string? Resolve() => Link.Element?.ReadAsciiString(Link.OffsetInElement);
}