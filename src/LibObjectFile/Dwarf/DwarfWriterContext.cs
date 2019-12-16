// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfWriterContext : DwarfReaderWriterContext
    {
        public DwarfWriterContext()
        {
            LayoutConfig = new DwarfLayoutConfig();
            EnableRelocation = true;
        }
        
        public DwarfLayoutConfig LayoutConfig { get; }

        public bool EnableRelocation { get; set; }
    }
}