// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

internal readonly struct PEImportFunctionTable()
{
    public List<PEImportFunctionEntry> Entries { get; } = new();

    public unsafe ulong CalculateSize(PEVisitorContext context)
    {
        var peFile = context.File;
        // +1 for the null terminator
        return (ulong)((Entries.Count + 1) * (peFile.IsPE32 ? sizeof(RawImportFunctionEntry32) : sizeof(RawImportFunctionEntry64)));
    }

    public void Read(PEImageReader reader, ulong position)
    {
        var peFile = reader.File;
        reader.Position = position;

        if (peFile.IsPE32)
        {
            Read32(reader);
        }
        else
        {
            Read64(reader);
        }
    }

    public void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        var entries = CollectionsMarshal.AsSpan(Entries);
        foreach (ref var entry in entries)
        {
            if (!entry.IsImportByOrdinal)
            {
                var va = entry.HintName.Offset;
                if (!peFile.TryFindVirtualContainer(va, out var container))
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

                entry = new PEImportFunctionEntry(new PEAsciiStringLink(streamSectionData, va - container.VirtualAddress));
            }
        }
    }

    private unsafe void Read32(PEImageReader reader)
    {
        RawImportFunctionEntry32 entry = default;
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref entry, 1));
        while (true)
        {
            int read = reader.Read(span);
            if (read != sizeof(RawImportFunctionEntry32))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidEndOfStream, $"Unable to read the full content of the Import Lookup Table. Expected {sizeof(RawImportFunctionEntry32)} bytes, but read {read} bytes");
                return;
            }

            if (entry.IsNull)
            {
                break;
            }

            Entries.Add(
                entry.IsImportByOrdinal
                    ? new PEImportFunctionEntry(entry.Ordinal)
                    : new PEImportFunctionEntry(new PEAsciiStringLink(PEStreamSectionData.Empty, entry.HintNameTableRVA))
            );
        }
    }

    private unsafe void Read64(PEImageReader reader)
    {
        RawImportFunctionEntry64 entry = default;
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref entry, 1));
        while (true)
        {
            int read = reader.Read(span);
            if (read != sizeof(RawImportFunctionEntry64))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidEndOfStream, $"Unable to read the full content of the Import Lookup Table. Expected {sizeof(RawImportFunctionEntry64)} bytes, but read {read} bytes");
                return;
            }

            if (entry.IsNull)
            {
                break;
            }

            Entries.Add(
                entry.IsImportByOrdinal
                    ? new PEImportFunctionEntry(entry.Ordinal)
                    : new PEImportFunctionEntry(new PEAsciiStringLink(PEStreamSectionData.Empty, entry.HintNameTableRVA))
            );
        }
    }

    public void Write(PEImageWriter writer)
    {
        if (writer.PEFile.IsPE32)
        {
            Write32(writer);
        }
        else
        {
            Write64(writer);
        }
    }

    private unsafe void Write32(PEImageWriter writer)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Entries.Count * sizeof(RawImportFunctionEntry32));
        try
        {
            var span = MemoryMarshal.Cast<byte, RawImportFunctionEntry32>(buffer.AsSpan(0, (Entries.Count + 1) * sizeof(RawImportFunctionEntry32)));
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var va = entry.HintName.RVA();
                span[i] = new RawImportFunctionEntry32(entry.IsImportByOrdinal ? 0x8000_0000U | entry.Ordinal : va);
            }

            // Last entry is null terminator
            span[^1] = default;

            writer.Write(MemoryMarshal.AsBytes(span));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private unsafe void Write64(PEImageWriter writer)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Entries.Count * sizeof(RawImportFunctionEntry64));
        try
        {
            var span = MemoryMarshal.Cast<byte, RawImportFunctionEntry64>(buffer.AsSpan(0, (Entries.Count + 1) * sizeof(RawImportFunctionEntry64)));
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var va = entry.HintName.RVA();
                span[i] = new RawImportFunctionEntry64(entry.IsImportByOrdinal ? 0x8000_0000_0000_0000UL | entry.Ordinal : va);
            }
            // Last entry is null terminator
            span[^1] = default;

            writer.Write(MemoryMarshal.AsBytes(span));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}