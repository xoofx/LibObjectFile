// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Enum representing the different types of debug information found in a PE file.
/// </summary>
public enum PEDebugKnownType
{
    /// <summary>
    /// An unknown value that is ignored by all tools.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The COFF debug information (line numbers, symbol table, and string table).
    /// This type of debug information is also pointed to by fields in the file headers.
    /// </summary>
    Coff = 1,

    /// <summary>
    /// The Visual C++ debug information.
    /// </summary>
    CodeView = 2,

    /// <summary>
    /// The frame pointer omission (FPO) information. This information tells the debugger
    /// how to interpret nonstandard stack frames, which use the EBP register for a purpose
    /// other than as a frame pointer.
    /// </summary>
    Fpo = 3,

    /// <summary>
    /// The location of a DBG file.
    /// </summary>
    Misc = 4,

    /// <summary>
    /// A copy of the .pdata section.
    /// </summary>
    Exception = 5,

    /// <summary>
    /// Reserved.
    /// </summary>
    Fixup = 6,

    /// <summary>
    /// The mapping from an RVA in the image to an RVA in the source image.
    /// </summary>
    OmapToSrc = 7,

    /// <summary>
    /// The mapping from an RVA in the source image to an RVA in the image.
    /// </summary>
    OmapFromSrc = 8,

    /// <summary>
    /// Reserved for Borland.
    /// </summary>
    Borland = 9,

    /// <summary>
    /// Reserved.
    /// </summary>
    Reserved10 = 10,

    /// <summary>
    /// Reserved.
    /// </summary>
    Clsid = 11,
    
    /// <summary>
    /// Visual C++ (CodeView
    /// </summary>
    VCFeature = 12,

    /// <summary>
    /// POGO
    /// </summary>
    POGO = 13,

    /// <summary>
    /// ILTCG
    /// </summary>
    ILTCG = 14,

    /// <summary>
    /// MPX
    /// </summary>
    MPX = 15,
    
    /// <summary>
    /// PE determinism or reproducibility.
    /// </summary>
    Repro = 16,

    /// <summary>
    /// Debugging information is embedded in the PE file at the location specified by PointerToRawData.
    /// </summary>
    EmbeddedRawData = 17,

    /// <summary>
    /// Undefined.
    /// </summary>
    SPGO = 18,
    
    /// <summary>
    /// Stores a cryptographic hash for the content of the symbol file used to build the PE/COFF file.
    /// </summary>
    SymbolFileHash = 19,

    /// <summary>
    /// Extended DLL characteristics bits.
    /// </summary>
    ExDllCharacteristics = 20
}