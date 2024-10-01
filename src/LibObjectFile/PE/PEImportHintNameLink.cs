// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a PE Import Hint Name in a <see cref="PEStreamSectionData"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PEImportHintNameLink(PEStreamSectionData? Container, RVO RVO) : IPELink<PEStreamSectionData, PEImportHintName>
{
    /// <inheritdoc />
    public override string ToString() => this.ToDisplayTextWithRVA();
    
    /// <summary>
    /// Resolves this link to a PE Import Hint Name.
    /// </summary>
    /// <returns>The PE Import Hint Name resolved.</returns>
    public PEImportHintName Resolve() => Container?.ReadHintName(RVO) ?? default;
}