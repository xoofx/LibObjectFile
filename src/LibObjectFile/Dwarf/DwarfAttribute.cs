// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public class DwarfAttribute : ObjectFilePart<DwarfDIE>
    {
        public DwarfAttributeName Name { get; set; }

        public DwarfAttributeValue Value { get; set; }
        
        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Value)}: {Value}";
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
        }
    }
}