// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

public sealed class PEBoundImportAddressTable32() : PEBoundImportAddressTable(true)
{
    public List<VA32> Entries { get; } = new();

    public override void Read(PEImageReader reader) => Read32(reader, Entries);

    public override void Write(PEImageWriter writer) => Write32(writer, Entries);

    public override int Count => Entries.Count;

    internal override void SetCount(int count) => CollectionsMarshal.SetCount(Entries, count);

    public override int ReadAt(uint offset, Span<byte> destination)
    {
        var buffer = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(Entries));
        return DataUtils.ReadAt(buffer, offset, destination);
    }

    public override void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        var buffer = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(Entries));
        DataUtils.WriteAt(buffer, offset, source);
    }

    public override unsafe bool CanReadWriteAt(uint offset, uint size) => offset + size <= (uint)Entries.Count * sizeof(VA32);
}