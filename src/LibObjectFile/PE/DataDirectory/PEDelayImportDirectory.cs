// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            if (!reader.File.TryFindSection(rawEntry.DelayLoadImportAddressTableRVA, out var section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidDelayLoadImportAddressTableRVA, $"Unable to find the section for DelayLoadImportAddressTableRVA {rawEntry.DelayLoadImportAddressTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importDelayLoadImportAddressTablePositionInFile = section.Position + rawEntry.DelayLoadImportAddressTableRVA - section.RVA;

            // DelayLoadImportNameTableRVA
            if (!reader.File.TryFindSection(rawEntry.DelayLoadImportNameTableRVA, out section))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidDelayLoadImportNameTableRVA, $"Unable to find the section for DelayLoadImportNameTableRVA {rawEntry.DelayLoadImportNameTableRVA}");
                return;
            }

            // Calculate its position within the original stream
            var importDelayLoadImportNameTablePositionInFile = section.Position + rawEntry.DelayLoadImportNameTableRVA - section.RVA;

            PEBoundImportAddressTable? boundImportAddressTable = null;
            if (rawEntry.BoundDelayLoadImportAddressTableRVA != 0)
            {
                // BoundDelayLoadImportAddressTableRVA
                if (!reader.File.TryFindSection(rawEntry.BoundDelayLoadImportAddressTableRVA, out section))
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidBoundDelayLoadImportAddressTableRVA, $"Unable to find the section for BoundDelayLoadImportAddressTableRVA {rawEntry.BoundDelayLoadImportAddressTableRVA}");
                    return;
                }

                boundImportAddressTable = is32Bits ? new PEBoundImportAddressTable32() : new PEBoundImportAddressTable64();
                boundImportAddressTable.Position = (uint)section.Position + rawEntry.BoundDelayLoadImportAddressTableRVA - section.RVA;
            }

            PEBoundImportAddressTable? unloadDelayInformationTable = null;
            if (rawEntry.UnloadDelayLoadImportAddressTableRVA != 0)
            {
                // UnloadDelayLoadImportAddressTableRVA
                if (!reader.File.TryFindSection(rawEntry.UnloadDelayLoadImportAddressTableRVA, out section))
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportDirectoryInvalidUnloadDelayLoadImportAddressTableRVA, $"Unable to find the section for UnloadDelayLoadImportAddressTableRVA {rawEntry.UnloadDelayLoadImportAddressTableRVA}");
                    return;
                }

                unloadDelayInformationTable = is32Bits ? new PEBoundImportAddressTable32() : new PEBoundImportAddressTable64();
                unloadDelayInformationTable.Position = (uint)section.Position + rawEntry.UnloadDelayLoadImportAddressTableRVA - section.RVA;
            }

            PEBoundImportAddressTable delayImportAddressTable = is32Bits ? new PEBoundImportAddressTable32() : new PEBoundImportAddressTable64();
            delayImportAddressTable.Position = (uint)importDelayLoadImportAddressTablePositionInFile;

            Entries.Add(

                new PEDelayImportDirectoryEntry(
                    new PEAsciiStringLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.NameRVA),
                    new PEModuleHandleLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.ModuleHandleRVA),
                    delayImportAddressTable,
                    new PEImportLookupTable()
                    {
                        Position = (uint)importDelayLoadImportNameTablePositionInFile,
                    }
                    )
                {
                    BoundImportAddressTable = boundImportAddressTable,
                    UnloadDelayInformationTable = unloadDelayInformationTable
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

            if (entry.BoundImportAddressTable != null)
            {
                entry.BoundImportAddressTable.SetCount(entry.DelayImportAddressTable.Count);
                entry.BoundImportAddressTable.Read(reader);
            }

            if (entry.UnloadDelayInformationTable != null)
            {
                entry.UnloadDelayInformationTable.SetCount(entry.DelayImportAddressTable.Count);
                entry.UnloadDelayInformationTable.Read(reader);
            }
        }
    }
    internal override IEnumerable<PESectionData> CollectImplicitSectionDataList()
    {
        foreach (var entry in Entries)
        {
            yield return entry.DelayImportAddressTable;
            yield return entry.DelayImportNameTable;

            if (entry.BoundImportAddressTable != null)
            {
                yield return entry.BoundImportAddressTable;
            }

            if (entry.UnloadDelayInformationTable != null)
            {
                yield return entry.UnloadDelayInformationTable;
            }
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
            if (!peFile.TryFindContainerByRVA(rva, out var container))
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

            rva = (RVA)(uint)entry.ModuleHandle.RVO;
            if (!peFile.TryFindContainerByRVA(rva, out container))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_DelayImportDirectoryInvalidModuleHandleRVA, $"Unable to find the section data for ModuleHandleRVA {rva}");
                return;
            }

            streamSectionData = container as PEStreamSectionData;
            if (streamSectionData is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_DelayImportDirectoryInvalidModuleHandleRVA, $"The section data for ModuleHandleRVA {rva} is not a stream section data");
                return;
            }

            entry.ModuleHandle = new PEModuleHandleLink(streamSectionData, rva - container.RVA);
            
            entry.DelayImportNameTable.FunctionTable.Bind(reader);
        }
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context) => CalculateSize();


    private unsafe uint CalculateSize()
    {
        return Entries.Count == 0 ? 0 : (uint)(((Entries.Count + 1) * sizeof(RawDelayLoadDescriptor)));
    }


    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}