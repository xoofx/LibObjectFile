// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public abstract class PEImportFunctionTable : PESectionData, IEnumerable<PEImportFunctionEntry>
{
    private protected PEImportFunctionTable(bool is32Bit)
    {
        Is32Bit = is32Bit;
    }

    public bool Is32Bit { get; }
    
    public List<PEImportFunctionEntry> Entries { get; } = new();

    public override bool HasChildren => false;

    protected override unsafe void UpdateLayoutCore(PELayoutContext context)
    {
        Size = Entries.Count == 0 ? 0 : (ulong)((Entries.Count + 1) * (Is32Bit ? sizeof(RawImportFunctionEntry32) : sizeof(RawImportFunctionEntry64)));
    }

    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;

        if (Is32Bit)
        {
            Read32(reader);
        }
        else
        {
            Read64(reader);
        }

        UpdateLayout(reader);
    }

    public override void Write(PEImageWriter writer)
    {
        if (Is32Bit)
        {
            Write32(writer);
        }
        else
        {
            Write64(writer);
        }
    }

    public void Add(PEImportFunctionEntry entry) => Entries.Add(entry);

    public List<PEImportFunctionEntry>.Enumerator GetEnumerator() => Entries.GetEnumerator();

    IEnumerator<PEImportFunctionEntry> IEnumerable<PEImportFunctionEntry>.GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();

    internal void Bind(PEImageReader reader, bool allowOutOfRange)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        var entries = CollectionsMarshal.AsSpan(Entries);
        for (var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];
            if (!entry.IsImportByOrdinal && !entry.IsLongOffset)
            {
                // The RVO is an RVA until we bind it to a container below
                var va = (RVA)(uint)entry.HintName.RVO;
                if (!peFile.TryFindByRVA(va, out var container))
                {
                    if (allowOutOfRange)
                    {
                        diagnostics.Warning(DiagnosticId.PE_WRN_ImportLookupTableInvalidRVAOutOfRange, $"Unable to find the section data for HintNameTableRVA {va}");
                        entry = new PEImportFunctionEntry(entry.UnsafeRawOffset);
                        continue;
                    }
                    else
                    {
                        diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidHintNameTableRVA, $"Unable to find the section data for HintNameTableRVA {va}");
                        //entry = new PEImportFunctionEntry(container.RVA);
                        return;
                    }
                }

                var streamSectionData = container as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidHintNameTableRVA, $"The section data for HintNameTableRVA {va} is not a stream section data");
                    return;
                }

                entry = new PEImportFunctionEntry(new PEImportHintNameLink(streamSectionData, va - container.RVA));
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
                    : new PEImportFunctionEntry(new PEImportHintNameLink(PEStreamSectionData.Empty, entry.HintNameTableRVA))
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
                    : entry.HintNameTableRVA > uint.MaxValue ? new PEImportFunctionEntry(entry.HintNameTableRVA) : new PEImportFunctionEntry(new PEImportHintNameLink(PEStreamSectionData.Empty, (uint)entry.HintNameTableRVA))
            );
        }
    }

    private unsafe void Write32(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RawImportFunctionEntry32>.Create(Entries.Count + 1, out var span);
        
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            var va = entry.HintName.RVA();
            span[i] = new RawImportFunctionEntry32(entry.IsImportByOrdinal ? 0x8000_0000U | entry.Ordinal : va);
        }

        // Last entry is null terminator
        span[^1] = default;

        writer.Write(tempSpan);
    }

    private unsafe void Write64(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RawImportFunctionEntry64>.Create(Entries.Count + 1, out var span);
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.IsLongOffset)
            {
                span[i] = new RawImportFunctionEntry64(entry.LongOffset);
            }
            else
            {
                span[i] = new RawImportFunctionEntry64(entry.IsImportByOrdinal ? 0x8000_0000_0000_0000UL | entry.Ordinal : entry.HintName.RVA());
            }
        }
        // Last entry is null terminator
        span[^1] = default;

        writer.Write(MemoryMarshal.AsBytes(span));
    }

    public override void Verify(PEVerifyContext context)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            entry.Verify(context, this, i);
        }
    }

    public override unsafe bool CanReadWriteAt(uint offset, uint size)
    {
        if (Is32Bit)
        {
            return offset + size <= (uint)Entries.Count * sizeof(RawImportFunctionEntry32);
        }
        else
        {
            return offset + size <= (uint)Entries.Count * sizeof(RawImportFunctionEntry64);
        }
    }

    public override unsafe int ReadAt(uint offset, Span<byte> destination)
    {
        var index = GetSafeIndex(offset);
        var entry = Entries[index];
        if (!entry.IsLongOffset)
        {
            throw new InvalidOperationException($"The function table entry at index {index} is not a long offset. Cannot from the 32-bit import function table {this}");
        }

        if (Is32Bit)
        {
            if (destination.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "Invalid size for reading data. The destination span must be 4 bytes long");
            }

            ref uint data = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(destination));
            data = (uint)entry.LongOffset;
            return 4;
        }
        else
        {
            if (destination.Length != 8)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "Invalid size for reading data. The destination span must be 8 bytes long");
            }

            ref ulong data = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(destination));
            data = entry.LongOffset;
            return 8;
        }
    }

    public override void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        var index = GetSafeIndex(offset);
        ref var entry = ref CollectionsMarshal.AsSpan(Entries)[index];
        if (!entry.IsLongOffset)
        {
            throw new InvalidOperationException($"The function table entry at index {index} is not a long offset. Cannot from the 32-bit import function table {this}");
        }

        if (Is32Bit)
        {
            if (source.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Invalid size for writing data. The source span must be 4 bytes long");
            }

            entry = new PEImportFunctionEntry(Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(source)));
        }
        else
        {
            if (source.Length != 8)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Invalid size for writing data. The source span must be 8 bytes long");
            }

            entry = new PEImportFunctionEntry(Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(source)));
        }
    }

    private unsafe int GetSafeIndex(uint offset)
    {
        if (Is32Bit)
        {
            var (index, remainder) = Math.DivRem((int)offset, sizeof(RawImportFunctionEntry32));
            if (remainder != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"The offset {offset} is not aligned on a {sizeof(RawImportFunctionEntry32)} boundary");
            }

            return index;
        }
        else
        {
            var (index, remainder) = Math.DivRem((int)offset, sizeof(RawImportFunctionEntry64));
            if (remainder != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"The offset {offset} is not aligned on a {sizeof(RawImportFunctionEntry64)} boundary");
            }

            return index;
        }
    }
}