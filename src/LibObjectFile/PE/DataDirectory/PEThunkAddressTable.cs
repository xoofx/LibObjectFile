// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

public abstract class PEThunkAddressTable : PESectionData
{
    protected PEThunkAddressTable(bool is32Bits)
    {
        Is32Bits = is32Bits;
    }

    public override bool HasChildren => false;

    public bool Is32Bits { get; }

    public abstract int Count { get; }

    internal abstract void SetCount(int count);


    private protected void Read32(PEImageReader reader, List<VA32> entries)
    {
        reader.Position = Position;

        // If the list have been preallocated, we read the actual count of entries directly
        if (entries.Count > 0)
        {
            var spanEntries = CollectionsMarshal.AsSpan(entries);
            foreach (ref var entry in spanEntries)
            {
                entry = reader.ReadU32();
            }

            var lastValue = reader.ReadU32();
            Debug.Assert(lastValue == 0);
        }
        else
        {
            while (true)
            {
                var thunk = reader.ReadU32();
                if (thunk == 0)
                {
                    break;
                }
                entries.Add(thunk);
            }
        }


        UpdateLayout(reader);
    }

    private protected void Read64(PEImageReader reader, List<VA64> entries)
    {
        reader.Position = Position;

        // If the list have been preallocated, we read the actual count of entries directly
        if (entries.Count > 0)
        {
            var spanEntries = CollectionsMarshal.AsSpan(entries);
            foreach (ref var entry in spanEntries)
            {
                entry = reader.ReadU64();
            }

            var lastValue = reader.ReadU64();
            Debug.Assert(lastValue == 0);
        }
        else
        {
            while (true)
            {
                var thunk = reader.ReadU64();
                if (thunk == 0)
                {
                    break;
                }

                entries.Add(thunk);
            }
        }

        UpdateLayout(reader);
    }

    private protected void Write32(PEImageWriter writer, List<VA32> entries)
    {
        writer.Position = Position;

        var span = CollectionsMarshal.AsSpan(entries);
        var bytes = MemoryMarshal.AsBytes(span);
        writer.Write(bytes);
        writer.WriteU32(0);
    }

    private protected void Write64(PEImageWriter writer, List<VA64> entries)
    {
        writer.Position = Position;

        var span = CollectionsMarshal.AsSpan(entries);
        var bytes = MemoryMarshal.AsBytes(span);
        writer.Write(bytes);
        writer.WriteU64(0);
    }

    public sealed override unsafe void UpdateLayout(PELayoutContext context)
    {
        Size = (uint)((Count + 1) * (Is32Bits ? sizeof(VA32) : sizeof(VA64)));
    }
}