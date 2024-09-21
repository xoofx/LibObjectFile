// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public sealed class PEImportDirectoryEntry
{
    public PEImportDirectoryEntry(PEAsciiStringLink importDllNameLink, PEImportAddressTable importAddressTable, PEImportLookupTable importLookupTable)
    {
        ImportDllNameLink = importDllNameLink;
        ImportAddressTable = importAddressTable;
        ImportLookupTable = importLookupTable;
    }

    public PEAsciiStringLink ImportDllNameLink { get; set; }

    public PEImportAddressTable ImportAddressTable { get; set; }

    public PEImportLookupTable ImportLookupTable { get; set; }
}