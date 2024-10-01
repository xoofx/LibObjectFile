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

/// <summary>
/// Represents the Bound Import Directory in a Portable Executable (PE) file.
/// </summary>
public sealed class PEBoundImportDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBoundImportDirectory"/> class.
    /// </summary>
    public PEBoundImportDirectory() : base(PEDataDirectoryKind.BoundImport)
    {
        Entries = new List<PEBoundImportDirectoryEntry>();
    }

    /// <summary>
    /// Gets the list of entries in the Bound Import Directory.
    /// </summary>
    public List<PEBoundImportDirectoryEntry> Entries { get; }

    /// <inheritdoc/>
    protected override unsafe uint ComputeHeaderSize(PELayoutContext context)
    {
        // Last raw entry is zero
        var size = (uint)sizeof(RawPEBoundImportDirectory);
        var entries = CollectionsMarshal.AsSpan(Entries);
        foreach (var entry in entries)
        {
            size += entry.Size;
        }

        return size;
    }

    /// <inheritdoc/>
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;

        var diagnostics = reader.Diagnostics;

        // Read Import Directory Entries
        RawPEBoundImportDirectory rawEntry = default;
        var entrySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref rawEntry, 1));

        while (true)
        {
            int read = reader.Read(entrySpan);
            if (read != entrySpan.Length)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidEndOfStream, $"Unable to read the full content of the Bound Import Directory. Expected {entrySpan.Length} bytes, but read {read} bytes");
                return;
            }

            // Check for null entry (last entry in the import directory)
            if (rawEntry.TimeDateStamp == 0 && rawEntry.OffsetModuleName == 0 && rawEntry.NumberOfModuleForwarderRefs == 0)
            {
                // Last entry
                break;
            }

            var entry = new PEBoundImportDirectoryEntry
            {
                // Store a fake entry, will be fully resolved at bind time
                ModuleName = new PEAsciiStringLink(PEStreamSectionData.Empty, rawEntry.OffsetModuleName)
            };

            if (rawEntry.NumberOfModuleForwarderRefs > 0)
            {
                using var tempSpan = TempSpan<RawPEBoundImportForwarderRef>.Create(rawEntry.NumberOfModuleForwarderRefs, out var spanForwarderRef);
                var span = tempSpan.AsBytes;

                read = reader.Read(span);
                if (read != span.Length)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidEndOfStream, $"Unable to read the full content of the Bound Import Directory. Expected {span.Length} bytes, but read {read} bytes");
                    return;
                }

                for (int i = 0; i < rawEntry.NumberOfModuleForwarderRefs; i++)
                {
                    var forwarderRef = spanForwarderRef[i];
                    // Store a fake entry, will be fully resolved at bind time
                    entry.ForwarderRefs.Add(new PEBoundImportForwarderRef(new PEAsciiStringLink(PEStreamSectionData.Empty, forwarderRef.OffsetModuleName)));
                }
            }
        }

        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);
    }

    /// <inheritdoc/>
    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        var entries = CollectionsMarshal.AsSpan(Entries);
        foreach (var entry in entries)
        {
            // The RVO is actually an RVA until we bind it here
            var va = (RVA)(uint)entry.ModuleName.RVO;
            if (!peFile.TryFindByRVA(va, out var container))
            {
                diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidModuleName, $"Unable to find the section data for ModuleName {va}");
                return;
            }

            var streamSectionData = container as PEStreamSectionData;
            if (streamSectionData is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidModuleName, $"The section data for ModuleName {va} is not a stream section data");
                return;
            }

            // Update the module name
            entry.ModuleName = new PEAsciiStringLink(streamSectionData, va - container.RVA);

            var forwarderRefs = CollectionsMarshal.AsSpan(entry.ForwarderRefs);
            foreach (ref var forwarderRef in forwarderRefs)
            {
                // The RVO is actually an RVA until we bind it here
                va = (RVA)(uint)forwarderRef.ModuleName.RVO;
                if (!peFile.TryFindByRVA(va, out container))
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidForwarderRefModuleName, $"Unable to find the section data for ForwarderRef ModuleName {va}");
                    return;
                }

                streamSectionData = container as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_BoundImportDirectoryInvalidForwarderRefModuleName, $"The section data for ForwarderRef ModuleName {va} is not a stream section data");
                    return;
                }

                // Update the forwarder ref module name
                forwarderRef = new(new PEAsciiStringLink(streamSectionData, va - container.RVA));
            }
        }
    }

    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        RawPEBoundImportDirectory rawEntry = default;
        RawPEBoundImportForwarderRef rawForwarderRef = default;
        foreach (var entry in Entries)
        {
            rawEntry.OffsetModuleName = (ushort)entry.ModuleName.RVO;
            rawEntry.NumberOfModuleForwarderRefs = (ushort)entry.ForwarderRefs.Count;
            writer.Write(rawEntry);

            foreach (var forwarderRef in entry.ForwarderRefs)
            {
                rawForwarderRef.OffsetModuleName = (ushort)forwarderRef.ModuleName.RVO;
                writer.Write(rawForwarderRef);
            }
        }

        // Last entry is null
        rawEntry = default;
        writer.Write(rawEntry);
    }
}