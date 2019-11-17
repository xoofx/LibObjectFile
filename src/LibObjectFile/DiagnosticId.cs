// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile
{
    /// <summary>
    /// Defines the various diagnostic message ids.
    /// </summary>
    public enum DiagnosticId
    {
        // Elf
        ELF_ERR_LinkOrInfoSectionNull = 2,
        ELF_ERR_LinkOrInfoInvalidSectionType = 3,
        ELF_ERR_LinkOrInfoInvalidSectionInstance = 4,
        ELF_ERR_InvalidHeaderFileClassNone = 5,
        ELF_ERR_InvalidHeaderIdentLength = 6,
        ELF_ERR_InvalidHeaderMagic = 7,
        //ELF_ERR_InvalidHeaderFileClass = 8,
        //ELF_ERR_InvalidHeaderEncoding = 9,
        ELF_ERR_MissingProgramHeaderTableSection = 10,
        ELF_ERR_InvalidSectionHeaderCount = 11,
        ELF_ERR_IncompleteHeader32Size = 12,
        ELF_ERR_IncompleteHeader64Size = 13,
        ELF_ERR_InvalidZeroProgramHeaderTableEntrySize = 14,
        ELF_ERR_InvalidProgramHeaderStreamOffset = 15,
        ELF_ERR_IncompleteProgramHeader32Size = 16,
        ELF_ERR_IncompleteProgramHeader64Size = 17,
        ELF_ERR_InvalidZeroSectionHeaderTableEntrySize = 18,
        ELF_ERR_InvalidSectionHeaderStreamOffset = 19,
        ELF_ERR_IncompleteSectionHeader32Size = 20,
        ELF_ERR_IncompleteSectionHeader64Size = 21,
        ELF_ERR_InvalidResolvedLink = 22,
        ELF_ERR_InvalidFirstSectionExpectingUndefined = 23,
        ELF_ERR_InvalidStringIndexMissingStringHeaderTable = 24,
        ELF_ERR_InvalidStringIndex = 25,
        ELF_ERR_InvalidOverlappingSections = 26,
        ELF_ERR_InvalidSegmentRange = 27,
        ELF_ERR_InvalidSectionSizeKind = 28,
        ELF_ERR_InvalidSectionLinkParent = 29,
        ELF_ERR_InvalidSectionInfoParent = 30,
        ELF_ERR_InvalidSegmentRangeBeginSectionParent = 31,
        ELF_ERR_InvalidSegmentRangeEndSectionParent = 32,
        ELF_ERR_InvalidSegmentRangeBeginOffset = 33,
        ELF_ERR_InvalidSegmentRangeEndOffset = 34,
        ELF_ERR_InvalidSegmentRangeIndices = 35,
        ELF_ERR_IncompleteRelocationAddendsEntry32Size = 36,
        ELF_ERR_IncompleteRelocationEntry32Size = 37,
        ELF_ERR_IncompleteRelocationAddendsEntry64Size = 38,
        ELF_ERR_IncompleteRelocationEntry64Size = 39,
        ELF_WRN_InvalidRelocationTablePrefixName = 40,
        ELF_WRN_InvalidRelocationTablePrefixTargetName = 41,
        ELF_ERR_InvalidRelocationInfoParent = 42,
        ELF_ERR_InvalidRelocationEntryAddend = 43,
        ELF_ERR_InvalidRelocationEntryArch = 44,
        ELF_ERR_InvalidRelocationSymbolIndex = 45,
        ELF_ERR_IncompleteSymbolEntry32Size = 46,
        ELF_ERR_IncompleteSymbolEntry64Size = 47,
        ELF_ERR_InvalidFirstSymbolEntryNonNull = 48,
        ELF_ERR_InvalidSymbolEntryNameIndex = 49,
        ELF_ERR_InvalidSymbolEntrySectionParent = 50,
        ELF_ERR_InvalidSymbolEntryLocalPosition = 51,
        ELF_ERR_IncompleteNoteEntrySize = 52,
        ELF_ERR_IncompleNoteGnuAbiTag = 53,
    }
}