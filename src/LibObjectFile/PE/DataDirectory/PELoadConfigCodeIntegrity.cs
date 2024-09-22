// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A structure representing the Code Integrity information in the Load Configuration Directory.
/// </summary>
public struct PELoadConfigCodeIntegrity
{
    public ushort Flags;          // Flags to indicate if CI information is available, etc.
    public ushort Catalog;        // 0xFFFF means not available
    public uint CatalogOffset;
    public uint Reserved;       // Additional bitmask to be defined later
}