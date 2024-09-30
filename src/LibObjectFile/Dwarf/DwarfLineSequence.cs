// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

/// <summary>
/// A sequence of <see cref="DwarfLine"/>
/// </summary>
[DebuggerDisplay("Count = {Lines.Count,nq}")]
public class DwarfLineSequence : DwarfObject<DwarfLineProgramTable>, IEnumerable<DwarfLine>
{
    private readonly ObjectList<DwarfLine> _lines;

    public DwarfLineSequence()
    {
        _lines = new ObjectList<DwarfLine>(this);
    }

    public ObjectList<DwarfLine> Lines => _lines;

    public void Add(DwarfLine line)
    {
        _lines.Add(line);
    }

    protected override void UpdateLayoutCore(DwarfLayoutContext layoutContext)
    {
        // This is implemented in DwarfLineSection
    }

    public override void Read(DwarfReader reader)
    {
        // This is implemented in DwarfLineSection
    }

    public override void Write(DwarfWriter writer)
    {
        // This is implemented in DwarfLineSection
    }

    public List<DwarfLine>.Enumerator GetEnumerator()
    {
        return _lines.GetEnumerator();
    }

    IEnumerator<DwarfLine> IEnumerable<DwarfLine>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _lines).GetEnumerator();
    }
}