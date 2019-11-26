// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{
    public class DwarfAbbreviationItem
    {
        public ulong Code { get; internal set; }

        public DwarfTag Tag { get; set; }

        public bool HasChildren { get; set; }

        public List<DwarfAttributeDescriptor> Descriptors { get; set; }
    }
}