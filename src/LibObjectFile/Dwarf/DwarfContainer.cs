// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfContainer : DwarfObject<DwarfContainer>
    {
        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }

        public DwarfFile GetParentFile()
        {
            var check = (ObjectFileNode)this;
            while (check != null)
            {
                if (check is DwarfFile dwarfFile) return dwarfFile;
                check = check.Parent;
            }
            return null;
        }

        public DwarfUnit GetParentUnit()
        {
            var check = (ObjectFileNode)this;
            while (check != null)
            {
                if (check is DwarfUnit dwarfUnit) return dwarfUnit;
                check = check.Parent;
            }
            return null;
        }
    }
}