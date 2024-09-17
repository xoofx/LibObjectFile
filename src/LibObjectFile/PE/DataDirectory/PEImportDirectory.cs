// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEImportDirectory : PEDirectory
{
    public PEImportDirectory() : base(ImageDataDirectoryKind.Import)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        var diagnostics = reader.Diagnostics;

        // Read Import Directory Entries
        RawImportDirectoryEntry entry = default;
        var entrySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref entry, 1));

        while (true)
        {
            int read = reader.Read(entrySpan);
            if (read != entrySpan.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidEndOfStream, $"Unable to read the full content of the Import Directory. Expected {entrySpan.Length} bytes, but read {read} bytes");
                return;
            }

            // Check for null entry (last entry in the import directory)
            if (entry.ImportLookupTableRVA == 0 && entry.TimeDateStamp == 0 && entry.ForwarderChain == 0 && entry.NameRVA == 0 && entry.ImportAddressTableRVA == 0)
            {
                // Last entry
                break;
            }
        }
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }



    //private struct HintNameTableEntry
    //{
    //    public ushort Hint;
    //    public byte Name1stByte;
    //}
}