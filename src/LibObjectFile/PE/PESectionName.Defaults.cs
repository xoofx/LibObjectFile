// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A section name in a Portable Executable (PE) image.
/// </summary>
partial record struct PESectionName
{
    /// <summary>
    /// Represents the .text section, which contains executable code.
    /// </summary>
    public static PESectionName Text => new(".text", false);

    /// <summary>
    /// Represents the .data section, which contains initialized data such as global variables.
    /// </summary>
    public static PESectionName Data => new(".data", false);

    /// <summary>
    /// Represents the .rdata section, which contains read-only initialized data.
    /// </summary>
    public static PESectionName RData => new(".rdata", false);

    /// <summary>
    /// Represents the .bss section, which contains uninitialized data.
    /// </summary>
    public static PESectionName Bss => new(".bss", false);

    /// <summary>
    /// Represents the .edata section, which contains the export directory.
    /// </summary>
    public static PESectionName EData => new(".edata", false);

    /// <summary>
    /// Represents the .idata section, which contains the import directory.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static PESectionName IData => new(".idata", false);

    /// <summary>
    /// Represents the .reloc section, which contains the base relocation table.
    /// </summary>
    public static PESectionName Reloc => new(".reloc", false);

    /// <summary>
    /// Represents the .rsrc section, which contains resources like icons, bitmaps, and strings.
    /// </summary>
    public static PESectionName Rsrc => new(".rsrc", false);

    /// <summary>
    /// Represents the .tls section, which contains thread-local storage (TLS) data.
    /// </summary>
    public static PESectionName Tls => new(".tls", false);

    /// <summary>
    /// Represents the .debug section, which contains debug information.
    /// </summary>
    public static PESectionName Debug => new(".debug", false);

    /// <summary>
    /// Represents the .pdata section, which contains exception-handling information for 64-bit code.
    /// </summary>
    public static PESectionName PData => new(".pdata", false);
}