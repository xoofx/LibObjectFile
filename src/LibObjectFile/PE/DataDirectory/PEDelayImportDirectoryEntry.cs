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

    public uint Attributes { get; set; }
    
    public PEAsciiStringLink DllName { get; set; }

    public PEModuleHandleLink ModuleHandle { get; set; }

    public PEBoundImportAddressTable DelayImportAddressTable { get; set; }

    public PEImportLookupTable DelayImportNameTable { get; set; }

    public PESectionDataLink BoundImportAddressTableLink { get; set; }

    public PESectionDataLink UnloadDelayInformationTableLink { get; set; }

    internal void Verify(PEVerifyContext context, PEDelayImportDirectory parent, int index)
    {
        context.VerifyObject(DllName.Container, parent, $"the {nameof(DllName)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", false);
        context.VerifyObject(ModuleHandle.Container, parent, $"the {nameof(ModuleHandle)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", false);
        context.VerifyObject(DelayImportAddressTable, parent, $"the {nameof(DelayImportAddressTable)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", false);
        context.VerifyObject(DelayImportNameTable, parent, $"the {nameof(DelayImportNameTable)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", false);
        // Allow null
        context.VerifyObject(BoundImportAddressTableLink.Container, parent, $"the {nameof(BoundImportAddressTableLink)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", true);
        context.VerifyObject(UnloadDelayInformationTableLink.Container, parent, $"the {nameof(UnloadDelayInformationTableLink)} of the {nameof(PEDelayImportDirectoryEntry)} at #{index}", true);
    }
}