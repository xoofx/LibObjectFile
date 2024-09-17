// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
public class PEImportLookupTable : PESectionData
{
    public List<PEImportLookupEntry> Entries { get; } = new();

    public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
    {
        var parent = Parent?.Parent;
        if (parent is null)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_InvalidParent, $"Parent is null for {nameof(PEImportLookupTable)} section data");
            return;
        }

        // +1 for the null terminator
        Size = (ulong)((Entries.Count + 1) * (parent.OptionalHeader.Magic == ImageOptionalHeaderMagic.PE32 ? sizeof(RawImportFunctionEntry32) : sizeof(RawImportFunctionEntry64)));
    }

    protected override unsafe void Read(PEImageReader reader)
    {
        var peFile = reader.PEFile;
        var diagnostics = reader.Diagnostics;

        if (peFile.OptionalHeader.Magic == ImageOptionalHeaderMagic.PE32)
        {
            while (true)
            {
                var entry = new RawImportFunctionEntry32(reader.ReadU32());

                if (entry.HintNameTableRVA == 0)
                {
                    break;
                }


                if (peFile.TryFindSection(entry.HintNameTableRVA, out var section))
                {

                }



                //Entries.Add(new PEImportLookupEntry(new ZeroTerminatedAsciiStringLink(new RVALink<PESectionData>(entry.HintNameTableRVA, this))));



            }

        }

    }

    protected override void Write(PEImageWriter writer)
    {
        if (writer.PEFile.OptionalHeader.Magic == ImageOptionalHeaderMagic.PE32)
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
            var span = MemoryMarshal.Cast<byte, RawImportFunctionEntry32>(buffer.AsSpan(0, Entries.Count * sizeof(RawImportFunctionEntry32)));
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var va = entry.FunctionNameLink.Link.VirtualAddress;
                span[i] = new RawImportFunctionEntry32(entry.IsImportByOrdinal ? 0x8000_0000U | entry.Ordinal : va);
            }

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
            var span = MemoryMarshal.Cast<byte, RawImportFunctionEntry64>(buffer.AsSpan(0, Entries.Count * sizeof(RawImportFunctionEntry64)));
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var va = entry.FunctionNameLink.Link.VirtualAddress;
                span[i] = new RawImportFunctionEntry64(entry.IsImportByOrdinal ? 0x8000_0000_0000_0000UL | entry.Ordinal : va);
            }

            writer.Write(MemoryMarshal.AsBytes(span));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}