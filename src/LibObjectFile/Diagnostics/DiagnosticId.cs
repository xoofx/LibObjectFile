// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Diagnostics;

/// <summary>
/// Defines the various diagnostic message ids.
/// </summary>
public enum DiagnosticId
{
    CMN_ERR_UnexpectedEndOfFile = 1,

    // Elf
    ELF_ERR_LinkOrInfoSectionNull = 102,
    ELF_ERR_LinkOrInfoInvalidSectionType = 103,
    ELF_ERR_LinkOrInfoInvalidSectionInstance = 104,
    ELF_ERR_InvalidHeaderFileClassNone = 105,
    ELF_ERR_InvalidHeaderIdentLength = 106,
    ELF_ERR_InvalidHeaderMagic = 107,
    //ELF_ERR_InvalidHeaderFileClass = 8,
    //ELF_ERR_InvalidHeaderEncoding = 9,
    ELF_ERR_MissingProgramHeaderTableSection = 110,
    ELF_ERR_InvalidSectionHeaderCount = 111,
    ELF_ERR_IncompleteHeader32Size = 112,
    ELF_ERR_IncompleteHeader64Size = 113,
    ELF_ERR_InvalidZeroProgramHeaderTableEntrySize = 114,
    ELF_ERR_InvalidProgramHeaderStreamOffset = 115,
    ELF_ERR_IncompleteProgramHeader32Size = 116,
    ELF_ERR_IncompleteProgramHeader64Size = 117,
    ELF_ERR_InvalidZeroSectionHeaderTableEntrySize = 118,
    ELF_ERR_InvalidSectionHeaderStreamOffset = 119,
    ELF_ERR_IncompleteSectionHeader32Size = 120,
    ELF_ERR_IncompleteSectionHeader64Size = 121,
    ELF_ERR_InvalidResolvedLink = 122,
    ELF_ERR_InvalidFirstSectionExpectingUndefined = 123,
    ELF_ERR_InvalidStringIndexMissingStringHeaderTable = 124,
    ELF_ERR_InvalidStringIndex = 125,
    ELF_ERR_InvalidOverlappingSections = 126,
    ELF_ERR_InvalidSegmentRange = 127,
    ELF_ERR_InvalidSectionSizeKind = 128,
    ELF_ERR_InvalidSectionLinkParent = 129,
    ELF_ERR_InvalidSectionInfoParent = 130,
    ELF_ERR_InvalidSegmentRangeBeginSectionParent = 131,
    ELF_ERR_InvalidSegmentRangeEndSectionParent = 132,
    ELF_ERR_InvalidSegmentRangeBeginOffset = 133,
    ELF_ERR_InvalidSegmentRangeEndOffset = 134,
    ELF_ERR_InvalidSegmentRangeIndices = 135,
    ELF_ERR_IncompleteRelocationAddendsEntry32Size = 136,
    ELF_ERR_IncompleteRelocationEntry32Size = 137,
    ELF_ERR_IncompleteRelocationAddendsEntry64Size = 138,
    ELF_ERR_IncompleteRelocationEntry64Size = 139,
    ELF_WRN_InvalidRelocationTablePrefixName = 140,
    ELF_WRN_InvalidRelocationTablePrefixTargetName = 141,
    ELF_ERR_InvalidRelocationInfoParent = 142,
    ELF_ERR_InvalidRelocationEntryAddend = 143,
    ELF_ERR_InvalidRelocationEntryArch = 144,
    ELF_ERR_InvalidRelocationSymbolIndex = 145,
    ELF_ERR_IncompleteSymbolEntry32Size = 146,
    ELF_ERR_IncompleteSymbolEntry64Size = 147,
    ELF_ERR_InvalidFirstSymbolEntryNonNull = 148,
    ELF_ERR_InvalidSymbolEntryNameIndex = 149,
    ELF_ERR_InvalidSymbolEntrySectionParent = 150,
    ELF_ERR_InvalidSymbolEntryLocalPosition = 151,
    ELF_ERR_IncompleteNoteEntrySize = 152,
    ELF_ERR_IncompleNoteGnuAbiTag = 153,
    ELF_ERR_InvalidSegmentVirtualAddressOrOffset = 154,
    ELF_ERR_InvalidSegmentAlignmentForLoad = 155,
    ELF_ERR_InvalidStreamForSectionNoBits = 156,
    ELF_ERR_InvalidNullSection = 157,
    ELF_ERR_InvalidAlignmentOutOfRange = 158,
    ELF_ERR_MissingSectionHeaderIndices = 159,
    ELF_ERR_MissingNullSection = 159,

    AR_ERR_InvalidMagicLength = 1000,
    AR_ERR_MagicNotFound = 1001,
    AR_ERR_ExpectingNewLineCharacter = 1002,
    //AR_ERR_UnexpectedEndOfFile = 1003,
    AR_ERR_InvalidFileEntryLength = 1004,
    AR_ERR_InvalidNonPrintableASCIIFoundInFileEntry = 1005,
    AR_ERR_InvalidCharacterFoundInFileEntry = 1006,
    AR_ERR_InvalidNullFileEntryName = 1007,
    AR_ERR_InvalidFileOffsetInSystemVSymbolLookupTable = 1008,
    AR_ERR_InvalidDuplicatedFutureHeadersTable = 1009,
    AR_ERR_InvalidReferenceToFutureHeadersTable = 1010,
    AR_ERR_InvalidFileEntryNameTooLong = 1011,
    AR_ERR_InvalidCharacterInFileEntryName = 1012,
    AR_ERR_InvalidNullOrEmptySymbolName = 1013,
    AR_ERR_InvalidNullFileForSymbol = 1014,
    AR_ERR_InvalidNullParentFileForSymbol = 1015,
    AR_ERR_InvalidParentFileForSymbol = 1016,
    AR_ERR_InvalidFileEntrySize = 1017,


    DWARF_ERR_AttributeLEB128OutOfRange = 2000,
    DWARF_ERR_VersionNotSupported = 2001,
    DWARF_ERR_InvalidData = 2002,
    DWARF_WRN_UnsupportedLineExtendedCode = 2003,
    DWARF_ERR_InvalidReference = 2004,
    DWARF_ERR_MissingStringTable = 2005,
    DWARF_ERR_InvalidNumberOfStandardOpCodeLengths = 2006,
    DWARF_ERR_InvalidStandardOpCodeLength = 2007,
    DWARF_WRN_CannotEncodeAddressIncrement = 2008,
    DWARF_ERR_InvalidNullFileNameEntry = 2009,
    DWARF_ERR_InvalidFileName = 2010,
    DWARF_ERR_InvalidMaximumOperationsPerInstruction = 2011,
    DWARF_ERR_InvalidNegativeAddressDelta = 2012,
    DWARF_ERR_InvalidOperationIndex = 2013,
    DWARF_ERR_InvalidAddressSize = 2014,
    DWARF_ERR_UnsupportedUnitType = 2015,
    DWARF_ERR_InvalidNullUnitForAddressRangeTable = 2016,
    DWARF_ERR_InvalidParentUnitForAddressRangeTable = 2017,
    DWARF_ERR_InvalidParentForDIE = 2018,
    DWARF_WRN_InvalidExtendedOpCodeLength = 2019,
    DWARF_ERR_InvalidParentForLocationList = 2020,

    // PE errors
    PE_ERR_InvalidDosHeaderSize = 3000,
    PE_ERR_InvalidDosHeaderMagic = 3001,
    PE_ERR_InvalidDosStubSize = 3002,
    PE_ERR_InvalidPESignature = 3003,
    PE_ERR_InvalidCoffHeaderSize = 3004,
    PE_ERR_InvalidOptionalHeaderSize = 3005,
    PE_ERR_InvalidOptionalHeaderMagic = 3006,
    PE_ERR_InvalidSectionHeadersSize = 3007,
    PE_ERR_InvalidParent = 3008,
    PE_ERR_InvalidExtraData = 3009,
    PE_ERR_SectionSizeLargerThanVirtualSize = 3010,
    PE_ERR_SectionRVALessThanPrevious = 3011,
    PE_ERR_TooManySections = 3012,
    PE_ERR_FileAlignmentNotPowerOfTwo = 3013,
    PE_ERR_SectionAlignmentNotPowerOfTwo = 3014,
    PE_ERR_SectionAlignmentLessThanFileAlignment = 3015,
    PE_ERR_InvalidPEHeaderPosition = 3016,
    PE_ERR_InvalidNumberOfDataDirectories = 3017,
    PE_ERR_InvalidBaseOfCode = 3018,
    PE_ERR_InvalidAddressOfEntryPoint = 3019,
    PE_ERR_DirectoryWithSameKindAlreadyAdded = 3020,
    PE_ERR_VerifyContextInvalidObject = 3021,
    PE_ERR_ChecksumNotSupported = 3022,

    // PE Exception directory
    PE_ERR_InvalidExceptionDirectory_Entries = 3100,
    PE_ERR_InvalidExceptionDirectory_Entry = 3101,
    PE_ERR_InvalidExceptionDirectory_Size = 3102,

    // PE Certificate directory
    PE_ERR_InvalidCertificateEntry = 3200,

    // PE BoundImport directory
    PE_ERR_BoundImportDirectoryInvalidEndOfStream = 3400,
    PE_ERR_BoundImportDirectoryInvalidModuleName = 3401,
    PE_ERR_BoundImportDirectoryInvalidForwarderRefModuleName = 3402,

    // PE DelayImport directory
    PE_ERR_ImportDirectoryInvalidDelayLoadImportAddressTableRVA = 3500,
    PE_ERR_ImportDirectoryInvalidDelayLoadImportNameTableRVA = 3501,
    PE_ERR_ImportDirectoryInvalidBoundDelayLoadImportAddressTableRVA = 3502,
    PE_ERR_ImportDirectoryInvalidUnloadDelayLoadImportAddressTableRVA = 3503,
    PE_ERR_ImportDirectoryInvalidNameRVA = 3504,
    PE_ERR_ImportDirectoryInvalidModuleHandleRVA = 3505,
    PE_ERR_DelayImportDirectoryInvalidDllNameRVA = 3506,
    PE_ERR_DelayImportDirectoryInvalidModuleHandleRVA = 3507,

    // PE Debug directory
    PE_ERR_DebugDirectorySize = 3600,
    PE_ERR_DebugDirectorySectionNotFound = 3601,
    PE_ERR_DebugDirectoryContainerNotFound = 3602,
    PE_ERR_InvalidDebugDataRSDSSignature = 3603,
    PE_ERR_InvalidDebugDataRSDSPdbPath = 3604,
    PE_ERR_DebugDirectoryExtraData = 3605,
    
    // PE BaseRelocation
    PE_ERR_BaseRelocationDirectoryInvalidEndOfStream = 3700,
    PE_ERR_BaseRelocationDirectoryInvalidSection = 3701,
    PE_ERR_BaseRelocationDirectoryInvalidSectionData = 3702,
    PE_ERR_InvalidDataDirectorySection = 3703,
    PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress = 3704,
    PE_ERR_BaseRelocationDirectoryInvalidSizeOfBlock = 3705,
    PE_ERR_InvalidBaseRelocationBlock = 3706,
    PE_ERR_BaseRelocationDirectoryInvalidSectionLink = 3707,
    PE_ERR_BaseRelocationInvalid = 3708,
    PE_WRN_BaseRelocationInVirtualMemory = 3709,

    // PE Import
    PE_ERR_ImportDirectoryInvalidEndOfStream = 3800,
    PE_ERR_ImportLookupTableInvalidEndOfStream = 3801,
    PE_ERR_ImportLookupTableInvalidHintNameTableRVA = 3802,
    PE_ERR_ImportLookupTableInvalidParent = 3803,
    PE_ERR_ImportDirectoryInvalidImportAddressTableRVA = 3804,
    PE_ERR_ImportDirectoryInvalidImportLookupTableRVA = 3805,
    PE_ERR_ImportAddressTableNotFound = 3806,
    PE_ERR_InvalidInternalState = 3807,
    PE_WRN_ImportLookupTableInvalidRVAOutOfRange = 3808,

    // PE Export
    PE_ERR_ExportAddressTableInvalidRVA = 3900,
    PE_ERR_ExportDirectoryInvalidAddressOfNames = 3901,
    PE_ERR_ExportDirectoryInvalidAddressOfFunctions = 3902,
    PE_ERR_ExportDirectoryInvalidAddressOfNameOrdinals = 3903,
    PE_ERR_ExportDirectoryInvalidName = 3904,
    PE_ERR_ExportNameTableInvalidRVA = 3905,

    // PE Resource directory
    PE_ERR_InvalidResourceDirectory = 4000,
    PE_ERR_InvalidResourceDirectoryEntry = 4001,
    PE_ERR_InvalidResourceDirectoryEntryRVAOffsetToData = 4002,
    PE_ERR_InvalidResourceString = 4003,
}