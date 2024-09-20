// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines directory entry indices.
/// </summary>
public enum PEDataDirectoryKind : ushort
{
    /// <summary>
    /// Export Directory
    /// </summary>
    Export = 0,
    /// <summary>
    /// Import Directory
    /// </summary>
    Import = 1,
    /// <summary>
    /// Resource Directory
    /// </summary>
    Resource = 2,
    /// <summary>
    /// Exception Directory
    /// </summary>
    Exception = 3,
    /// <summary>
    /// Security Directory
    /// </summary>
    Security = 4,
    /// <summary>
    /// Base Relocation Table
    /// </summary>
    BaseRelocation = 5,
    /// <summary>
    /// Debug Directory
    /// </summary>
    Debug = 6,
    /// <summary>
    /// Architecture Specific Data
    /// </summary>
    Architecture = 7,
    /// <summary>
    /// RVA of GP
    /// </summary>
    GlobalPointer = 8,
    /// <summary>
    /// TLS Directory
    /// </summary>
    Tls = 9,
    /// <summary>
    /// Load Configuration Directory
    /// </summary>
    LoadConfig = 10,
    /// <summary>
    /// Bound Import Directory in headers
    /// </summary>
    BoundImport = 11,
    /// <summary>
    /// Import Address Table
    /// </summary>
    ImportAddressTable = 12,
    /// <summary>
    /// Delay Load Import Descriptors
    /// </summary>
    DelayImport = 13,
    /// <summary>
    /// .NET CLR Metadata
    /// </summary>
    ClrMetadata = 14,
}