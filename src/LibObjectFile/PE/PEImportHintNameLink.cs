// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A link to a PE Import Hint Name in a <see cref="PEStreamSectionData"/>.
/// </summary>
/// <param name="Link">The link.</param>
public readonly record struct PEImportHintNameLink(RVALink<PEStreamSectionData> Link)
{
    /// <summary>
    /// Gets a value indicating whether this instance is null.
    /// </summary>
    public bool IsNull => Link.IsNull;

    /// <summary>
    /// Resolves this link to a PE Import Hint Name.
    /// </summary>
    /// <returns>The PE Import Hint Name resolved.</returns>
    public PEImportHintName Resolve() => Link.Element is null ? default : Link.Element.ReadHintName(Link.OffsetInElement);
}