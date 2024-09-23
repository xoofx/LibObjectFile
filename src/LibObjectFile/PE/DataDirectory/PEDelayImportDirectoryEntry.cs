// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public class PEDelayImportDirectoryEntry
{
    public PEDelayImportDirectoryEntry(PEAsciiStringLink dllName, PEModuleHandleLink moduleHandle, PEBoundImportAddressTable delayImportAddressTable, PEImportLookupTable delayImportNameTable)
    {
        DllName = dllName;
        ModuleHandle = moduleHandle;
        DelayImportAddressTable = delayImportAddressTable;
        DelayImportNameTable = delayImportNameTable;
    }
    
    public PEAsciiStringLink DllName { get; set; }

    public PEModuleHandleLink ModuleHandle { get; set; }

    public PEBoundImportAddressTable DelayImportAddressTable { get; set; }

    public PEImportLookupTable DelayImportNameTable { get; set; }

    public PEBoundImportAddressTable? BoundImportAddressTable { get; set; }

    public PEBoundImportAddressTable? UnloadDelayInformationTable { get; set; }
}