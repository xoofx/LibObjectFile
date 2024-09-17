// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

#pragma warning disable CS0649
public class PEImportDirectoryEntry
{
    public ZeroTerminatedAsciiStringLink ImportDllNameLink;

    public uint ImportAddressTableEntryIndex;

}