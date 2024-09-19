// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEImportDirectory : PEDirectory
{
    private readonly ObjectList<PEImportDirectoryEntry> _entries;

    public PEImportDirectory() : base(ImageDataDirectoryKind.Import, false)
    {
        _entries = new(this);
    }
    public ObjectList<PEImportDirectoryEntry> Entries => _entries;

    public override void UpdateLayout(PEVisitorContext context)
    {
        UpdateSize();
    }

    public override void Read(PEImageReader reader)
    {
        var diagnostics = reader.Diagnostics;

        reader.Position = Position;

        // Read Import Directory Entries
        RawImportDirectoryEntry rawEntry = default;
        var entrySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref rawEntry, 1));

        while (true)
        {
            int read = reader.Read(entrySpan);
            if (read != entrySpan.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidEndOfStream, $"Unable to read the full content of the Import Directory. Expected {entrySpan.Length} bytes, but read {read} bytes");
                return;
            }

            // TODO: handle bound imports through entry.TimeDateStamp

            // Check for null entry (last entry in the import directory)
            if (rawEntry.ImportLookupTableRVA == 0 && rawEntry.TimeDateStamp == 0 && rawEntry.ForwarderChain == 0 && rawEntry.NameRVA == 0 && rawEntry.ImportAddressTableRVA == 0)
            {
                // Last entry
                break;
            }

            // Find the section data for the ImportLookupTableRVA
            if (!reader.File.TryFindSection(rawEntry.ImportAddressTableRVA, out var section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidImportAddressTableRVA, $"Unable to find the section data for ImportAddressTableRVA {rawEntry.ImportAddressTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importLookupAddressTablePositionInFile = section.Position + rawEntry.ImportLookupTableRVA - section.VirtualAddress;

            // Find the section data for the ImportLookupTableRVA
            if (!reader.File.TryFindSection(rawEntry.ImportLookupTableRVA, out section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidImportLookupTableRVA, $"Unable to find the section data for ImportLookupTableRVA {rawEntry.ImportLookupTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importLookupTablePositionInFile = section.Position + rawEntry.ImportLookupTableRVA - section.VirtualAddress;
            
            // Store a fake entry for post-processing section data to allow to recreate PEImportLookupTable from existing PESectionStreamData
            _entries.Add(
                new PEImportDirectoryEntry(
                    // Name
                    new(new(PETempSectionData.Instance, rawEntry.NameRVA)),
                    // ImportAddressTable
                    new PEImportAddressTable()
                    {
                        Position = importLookupAddressTablePositionInFile
                    },
                    // ImportLookupTable
                    new PEImportLookupTable()
                    {
                        Position = importLookupTablePositionInFile
                    }
                )
            );
        }

        UpdateSize();

        // Resolve ImportLookupTable and ImportAddressTable section data links
        var entries = CollectionsMarshal.AsSpan(_entries.UnsafeList);
        foreach (ref var entry in entries)
        {
            entry.ImportAddressTable.Read(reader);
            entry.ImportLookupTable.Read(reader);
        }
    }

    private unsafe void UpdateSize()
    {
        Size = (ulong)((_entries.Count + 1) * sizeof(RawImportDirectoryEntry));
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }



    //private struct HintNameTableEntry
    //{
    //    public ushort Hint;
    //    public byte Name1stByte;
    //}
}