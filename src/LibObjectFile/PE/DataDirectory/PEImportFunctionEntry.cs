// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A PE Import Function Entry used in <see cref="PEImportLookupTable"/> and <see cref="PEImportAddressTable"/>.
/// </summary>
public readonly struct PEImportFunctionEntry
{
    // Encodes the RVA through a link to the PE section data and the offset in the section data
    // If the PE section data is null, the offset is the ordinal
    private readonly PEStreamSectionData? _peSectionData;
    private readonly uint _offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PEImportFunctionEntry"/> class by name.
    /// </summary>
    /// <param name="name">The name of the import.</param>
    public PEImportFunctionEntry(PEAsciiStringLink name)
    {
        _peSectionData = name.StreamSectionData;
        _offset = name.RVO;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEImportFunctionEntry"/> class by ordinal.
    /// </summary>
    /// <param name="ordinal">The ordinal of the import.</param>
    public PEImportFunctionEntry(ushort ordinal)
    {
        _peSectionData = null;
        _offset = ordinal;
    }

    /// <summary>
    /// Gets a value indicating whether this import is by ordinal.
    /// </summary>
    public bool IsImportByOrdinal => _peSectionData is null;
    
    /// <summary>
    /// Gets the name of the import if not by ordinal.
    /// </summary>
    public PEImportHintNameLink HintName => _peSectionData is null ? default : new PEImportHintNameLink(_peSectionData, _offset);

    /// <summary>
    /// Gets the ordinal of the import if by ordinal.
    /// </summary>
    public ushort Ordinal => _peSectionData is null ? (ushort)(_offset) : (ushort)0;
}
