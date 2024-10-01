// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEImportDirectory : PEDataDirectory
{
    private readonly List<PEImportDirectoryEntry> _entries;

    public PEImportDirectory() : base(PEDataDirectoryKind.Import)
    {
        _entries = new();
    }

    public List<PEImportDirectoryEntry> Entries => _entries;
    
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
            if (!reader.File.TryFindSectionByRVA(rawEntry.ImportAddressTableRVA, out var section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidImportAddressTableRVA, $"Unable to find the section data for ImportAddressTableRVA {rawEntry.ImportAddressTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importLookupAddressTablePositionInFile = section.Position + rawEntry.ImportAddressTableRVA - section.RVA;

            // Find the section data for the ImportLookupTableRVA
            if (!reader.File.TryFindSectionByRVA(rawEntry.ImportLookupTableRVA, out section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidImportLookupTableRVA, $"Unable to find the section data for ImportLookupTableRVA {rawEntry.ImportLookupTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importLookupTablePositionInFile = section.Position + rawEntry.ImportLookupTableRVA - section.RVA;
            
            // Store a fake entry for post-processing section data to allow to recreate PEImportLookupTable from existing PESectionStreamData
            _entries.Add(
                new PEImportDirectoryEntry(
                    // Name
                    new(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.NameRVA), // Store the RVA as a fake RVO until we bind it in the Bind phase
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
                {
                    TimeDateStamp = rawEntry.TimeDateStamp,
                    ForwarderChain = rawEntry.ForwarderChain
                }
            );
        }

        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);

        // Resolve ImportLookupTable and ImportAddressTable section data links
        var entries = CollectionsMarshal.AsSpan(_entries);
        foreach (ref var entry in entries)
        {
            entry.ImportAddressTable.Read(reader);
            entry.ImportLookupTable.Read(reader);
        }
    }

    public override void Write(PEImageWriter writer)
    {
        RawImportDirectoryEntry rawEntry = default;
        foreach (var entry in Entries)
        {
            rawEntry.NameRVA = (uint)entry.ImportDllNameLink.RVA();
            rawEntry.ImportLookupTableRVA = (uint)entry.ImportLookupTable.RVA;
            rawEntry.ImportAddressTableRVA = (uint)entry.ImportAddressTable.RVA;
            rawEntry.TimeDateStamp = entry.TimeDateStamp;
            rawEntry.ForwarderChain = entry.ForwarderChain;
            writer.Write(rawEntry);
        }

        // Null entry
        rawEntry = default;
        writer.Write(rawEntry);
    }

    protected override unsafe uint ComputeHeaderSize(PELayoutContext context) => CalculateSize();

    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
    {
        foreach (var entry in _entries)
        {
            yield return entry.ImportAddressTable;
            yield return entry.ImportLookupTable;
        }
    }

    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        var entries = CollectionsMarshal.AsSpan(_entries);
        foreach (ref var entry in entries)
        {
            // The RVO is actually an RVA until we bind it here
            var va = (RVA)(uint)entry.ImportDllNameLink.RVO;
            if (!peFile.TryFindByRVA(va, out var container))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidHintNameTableRVA, $"Unable to find the section data for HintNameTableRVA {va}");
                return;
            }

            var streamSectionData = container as PEStreamSectionData;
            if (streamSectionData is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidHintNameTableRVA, $"The section data for HintNameTableRVA {va} is not a stream section data");
                return;
            }

            entry = new PEImportDirectoryEntry(
                new PEAsciiStringLink(streamSectionData, va - container.RVA),
                entry.ImportAddressTable,
                entry.ImportLookupTable)
            {
                TimeDateStamp = entry.TimeDateStamp,
                ForwarderChain = entry.ForwarderChain
            };
        }


        foreach (var entry in Entries)
        {
            entry.ImportAddressTable.FunctionTable.Bind(reader, true);
            entry.ImportLookupTable.FunctionTable.Bind(reader, false);
        }
    }

    public override void Verify(PEVerifyContext context)
    {
        var entries = CollectionsMarshal.AsSpan(_entries);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            entry.Verify(context, this, i);
        }
        
        base.Verify(context);
    }

    private unsafe uint CalculateSize()
    {
        return (uint)(((_entries.Count + 1) * sizeof(RawImportDirectoryEntry)));
    }
}