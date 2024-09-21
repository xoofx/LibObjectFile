// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A link to a PE Import Hint Name in a <see cref="PEStreamSectionData"/>.
/// </summary>
public readonly struct PEImportHintNameLink : RVALink<PEImportHintName>
{
    public PEImportHintNameLink(PEStreamSectionData? streamSectionData, uint offsetInSection)
    {
        StreamSectionData = streamSectionData;
        Offset = offsetInSection;
    }

    public readonly PEStreamSectionData? StreamSectionData;

    public PEVirtualObject? Container => StreamSectionData;

    public uint Offset { get; }

    /// <inheritdoc />
    public override string ToString() => this.ToDisplayText();
    
    /// <summary>
    /// Resolves this link to a PE Import Hint Name.
    /// </summary>
    /// <returns>The PE Import Hint Name resolved.</returns>
    public PEImportHintName Resolve() => StreamSectionData?.ReadHintName(Offset) ?? default;
}