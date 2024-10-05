// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System.Xml.Linq;

namespace LibObjectFile.Elf;

public class ElfVisitorContext : VisitorContextBase<ElfFile>
{
    internal ElfVisitorContext(ElfFile elfFile, DiagnosticBag diagnostics) : base(elfFile, diagnostics)
    {
    }

    public ElfString ResolveName(ElfString name)
    {
        var stringTable = File.SectionHeaderStringTable;
        if (stringTable is null)
        {
            Diagnostics.Error(DiagnosticId.ELF_ERR_SectionHeaderStringTableNotFound, $"The section header string table is not found. Cannot resolve {name}.");
            return name;
        }

        return stringTable.Resolve(name);
    }
}