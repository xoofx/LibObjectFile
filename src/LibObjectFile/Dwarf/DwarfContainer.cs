// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfContainer : ObjectFileNode<DwarfContainer>
    {
        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            return true;
        }

        public DwarfFile GetParentFile()
        {
            var check = this;
            while (check != null)
            {
                if (check is DwarfFile dwarfFile) return dwarfFile;
                check = check.Parent;
            }
            return null;
        }
    }
}