// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public static class DwarfPrinter
    {
        public static void Print(this DwarfDebugAbbrevTable abbrevTable, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine("Contents of the .debug_abbrev section:");
            
            foreach (var abbreviation in abbrevTable.Abbreviations)
            {
                Print(abbreviation, writer);
            }
        }

        public static void Print(this DwarfAbbreviation abbreviation, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();

            writer.WriteLine($"  Number TAG (0x{abbreviation.Offset})");

            foreach (var item in abbreviation.Items)
            {
                writer.WriteLine($"   {item.Code}      {item.Tag}    [{(item.HasChildren ? "has children" : "no children")}]");
                var descriptors = item.Descriptors;
                for (int i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];
                    writer.WriteLine($"    {descriptor.Kind.ToString(),-18} {descriptor.Form}");
                }
                writer.WriteLine("    DW_AT value: 0     DW_FORM value: 0");
            }
        }
    }
}