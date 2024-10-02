// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public sealed class PEDelayImportDirectory : PEDataDirectory
{
    public PEDelayImportDirectory() : base(PEDataDirectoryKind.DelayImport)
    {
        Entries = new List<PEDelayImportDirectoryEntry>();
    }

    public List<PEDelayImportDirectoryEntry> Entries { get; }

    public override void Read(PEImageReader reader)
    {
        var diagnostics = reader.Diagnostics;

        bool is32Bits = reader.File.IsPE32;
        reader.Position = Position;

        // Read Import Directory Entries
        RawDelayLoadDescriptor rawEntry = default;
        var entrySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref rawEntry, 1));

        while (true)
        {
            int read = reader.Read(entrySpan);
            if (read != entrySpan.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidEndOfStream, $"Unable to read the full content of the Import Directory. Expected {entrySpan.Length} bytes, but read {read} bytes");
                return;
            }

            // Check for null entry (last entry in the import directory)
            if (rawEntry.Attributes == 0 && rawEntry.NameRVA == 0 && rawEntry.DelayLoadImportAddressTableRVA == 0 && rawEntry.DelayLoadImportNameTableRVA == 0 && rawEntry.BoundDelayLoadImportAddressTableRVA == 0 && rawEntry.UnloadDelayLoadImportAddressTableRVA == 0 && rawEntry.TimeDateStamp == 0)
            {
                // Last entry
                break;
            }
            
            // DelayLoadImportAddressTableRVA
            if (!reader.File.TryFindSectionByRVA(rawEntry.DelayLoadImportAddressTableRVA, out var section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidDelayLoadImportAddressTableRVA, $"Unable to find the section for DelayLoadImportAddressTableRVA {rawEntry.DelayLoadImportAddressTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importDelayLoadImportAddressTablePositionInFile = section.Position + rawEntry.DelayLoadImportAddressTableRVA - section.RVA;

            // DelayLoadImportNameTableRVA
            if (!reader.File.TryFindSectionByRVA(rawEntry.DelayLoadImportNameTableRVA, out section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidDelayLoadImportNameTableRVA, $"Unable to find the section for DelayLoadImportNameTableRVA {rawEntry.DelayLoadImportNameTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importDelayLoadImportNameTablePositionInFile = section.Position + rawEntry.DelayLoadImportNameTableRVA - section.RVA;

            PEBoundImportAddressTable delayImportAddressTable = is32Bits ? new PEBoundImportAddressTable32() : new PEBoundImportAddressTable64();
            delayImportAddressTable.Position = (uint)importDelayLoadImportAddressTablePositionInFile;

            PEImportLookupTable lookupTable = is32Bits ? new PEImportLookupTable32() : new PEImportLookupTable64();
            lookupTable.Position = (uint)importDelayLoadImportNameTablePositionInFile;

            Entries.Add(

                new PEDelayImportDirectoryEntry(
                    new PEAsciiStringLink(null, (RVO)(uint)rawEntry.NameRVA),
                    new PEModuleHandleLink(null, (RVO)(uint)rawEntry.ModuleHandleRVA),
                    delayImportAddressTable,
                    lookupTable
                    )
                {
                    Attributes = rawEntry.Attributes,
                    BoundImportAddressTableLink = new PESectionDataLink(null, (RVO)(uint)rawEntry.BoundDelayLoadImportAddressTableRVA),
                    UnloadDelayInformationTableLink = new PESectionDataLink(null, (RVO)(uint)rawEntry.UnloadDelayLoadImportAddressTableRVA)
                }
            );
        }

        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);

        // Resolve ImportLookupTable and ImportAddressTable section data links
        var entries = CollectionsMarshal.AsSpan(Entries);
        foreach (ref var entry in entries)
        {
            entry.DelayImportAddressTable.Read(reader);
            entry.DelayImportNameTable.Read(reader);
        }
    }
    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
    {
        foreach (var entry in Entries)
        {
            yield return entry.DelayImportAddressTable;
            yield return entry.DelayImportNameTable;
        }
    }

    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        var entries = CollectionsMarshal.AsSpan(Entries);

        foreach (ref var entry in entries)
        {
            var rva = (RVA)(uint)entry.DllName.RVO;
            if (!peFile.TryFindByRVA(rva, out var container))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_DelayImportDirectoryInvalidDllNameRVA, $"Unable to find the section data for DllNameRVA {rva}");
                return;
            }

            var streamSectionData = container as PEStreamSectionData;
            if (streamSectionData is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_DelayImportDirectoryInvalidDllNameRVA, $"The section data for DllNameRVA {rva} is not a stream section data");
                return;
            }

            entry.DllName = new PEAsciiStringLink(streamSectionData, rva - container.RVA);

            // The ModuleHandle could be in Virtual memory and not bound, so we link to a section and not a particular data on the disk
            rva = (RVA)(uint)entry.ModuleHandle.RVO;
            if (!peFile.TryFindSectionByRVA(rva, out var moduleSection))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_DelayImportDirectoryInvalidModuleHandleRVA, $"Unable to find the section data for ModuleHandleRVA {rva}");
                return;
            }

            entry.ModuleHandle = new PEModuleHandleLink(moduleSection, rva - moduleSection.RVA);
            
            entry.DelayImportNameTable.Bind(reader, false);


            if (entry.BoundImportAddressTableLink.RVO != 0)
            {
                rva = (RVA)(uint)entry.BoundImportAddressTableLink.RVO;
                if (!peFile.TryFindByRVA(rva, out container))
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidBoundDelayLoadImportAddressTableRVA, $"Unable to find the section data for BoundImportAddressTableRVA {rva}");
                    return;
                }

                streamSectionData = container as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidBoundDelayLoadImportAddressTableRVA, $"The section data for BoundImportAddressTableRVA {rva} is not a stream section data");
                    return;
                }

                entry.BoundImportAddressTableLink = new PESectionDataLink(streamSectionData, rva - container.RVA);
            }

            if (entry.UnloadDelayInformationTableLink.RVO != 0)
            {
                rva = (RVA)(uint)entry.UnloadDelayInformationTableLink.RVO;
                if (!peFile.TryFindByRVA(rva, out container))
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidUnloadDelayLoadImportAddressTableRVA, $"Unable to find the section data for UnloadDelayInformationTableRVA {rva}");
                    return;
                }

                streamSectionData = container as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidUnloadDelayLoadImportAddressTableRVA, $"The section data for UnloadDelayInformationTableRVA {rva} is not a stream section data");
                    return;
                }

                entry.UnloadDelayInformationTableLink = new PESectionDataLink(streamSectionData, rva - container.RVA);
            }
        }
    }

    protected override uint ComputeHeaderSize(PELayoutContext context) => CalculateSize();


    private unsafe uint CalculateSize()
    {
        return Entries.Count == 0 ? 0 : (uint)(((Entries.Count + 1) * sizeof(RawDelayLoadDescriptor)));
    }


    public override unsafe void Write(PEImageWriter writer)
    {
        var entries = CollectionsMarshal.AsSpan(Entries);
        using var tempSpan = TempSpan<RawDelayLoadDescriptor>.Create(entries.Length + 1, out var rawEntries);

        RawDelayLoadDescriptor rawEntry = default;
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            rawEntry.Attributes = entry.Attributes;
            rawEntry.NameRVA = (uint)entry.DllName.RVA();
            rawEntry.ModuleHandleRVA = (uint)entry.ModuleHandle.RVA();
            rawEntry.DelayLoadImportAddressTableRVA = (uint)entry.DelayImportAddressTable.RVA;
            rawEntry.DelayLoadImportNameTableRVA = (uint)entry.DelayImportNameTable.RVA;
            rawEntry.BoundDelayLoadImportAddressTableRVA = entry.BoundImportAddressTableLink.RVA();
            rawEntry.UnloadDelayLoadImportAddressTableRVA = entry.UnloadDelayInformationTableLink.RVA();
            rawEntry.TimeDateStamp = 0;

            rawEntries[i] = rawEntry;
        }

        // Write the null entry
        rawEntries[entries.Length] = default;

        writer.Write(tempSpan.AsBytes);
    }

    public override void Verify(PEVerifyContext context)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            entry.Verify(context, this, i);
        }

        base.Verify(context);
    }
}