// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

public sealed class PEBoundImportAddressTable64() : PEBoundImportAddressTable(false)
{
    public List<VA64> Entries { get; } = new();

    public override void Read(PEImageReader reader) => Read64(reader, Entries);

    public override void Write(PEImageWriter writer) => Write64(writer, Entries);

    public override int Count => Entries.Count;

    internal override void SetCount(int count) => CollectionsMarshal.SetCount(Entries, count);
}