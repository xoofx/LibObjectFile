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

    /// <summary>
    /// The stamp that is set to zero until the image is bound. After the image is bound, this field is set to the time/data stamp of the DLL.
    ///
    /// 0 if not bound,
    /// -1 if bound, and real date\time stamp in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND)
    /// O.W. date/time stamp of DLL bound to (Old BIND)
    /// </summary>
    public uint TimeDateStamp { get; set; }

    /// <summary>
    /// The index of the first forwarder reference. -1 if no forwarders
    /// </summary>
    public uint ForwarderChain { get; set; }
}