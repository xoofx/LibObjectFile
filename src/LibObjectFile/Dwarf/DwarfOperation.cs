// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DwarfOperation : ObjectFileNode<DwarfExpression>
    {
        public DwarfOperationKindEx Kind { get; set; }

        public object Operand0 { get; set; }

        public DwarfInteger Operand1;

        public DwarfInteger Operand2;

        private string DebuggerDisplay => $"{Kind} {Operand1} {Operand2} {Operand0}";

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}