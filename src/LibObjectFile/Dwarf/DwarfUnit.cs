// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfUnit : DwarfContainer, IRelocatable
    {
        public ulong GetRelocatableValue(ulong relativeOffset, RelocationSize size)
        {
            throw new NotImplementedException();
        }

        public void SetRelocatableValue(ulong relativeOffset, RelocationSize size)
        {
            throw new NotImplementedException();
        }
    }
}