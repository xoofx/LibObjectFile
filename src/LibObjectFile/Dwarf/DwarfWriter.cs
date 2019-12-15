// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public class DwarfWriter : DwarfReaderWriter
    {
        internal DwarfWriter(DwarfFile file, DwarfWriterContext context, DiagnosticBag diagnostics) : base(file, diagnostics)
        {
        }

        public override bool IsReadOnly => false;
    }
}