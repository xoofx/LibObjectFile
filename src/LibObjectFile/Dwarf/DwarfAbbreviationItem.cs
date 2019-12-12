// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAbbreviationItem : ObjectFileNode<DwarfAbbreviation>
    {
        internal DwarfAbbreviationItem(ulong code, DwarfAbbreviation parent, DwarfTagEx tag, bool hasChildren, DwarfAttributeDescriptors descriptors)
        {
            Code = code;
            Parent = parent;
            Tag = tag;
            HasChildren = hasChildren;
            Descriptors = descriptors;
        }
        
        public ulong Code { get; }

        public DwarfTagEx Tag { get; }

        public bool HasChildren { get; }

        public DwarfAttributeDescriptors Descriptors { get; }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

    }
}