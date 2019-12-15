// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public struct DwarfFileLayout
    {
        private DwarfFile _parent;
        private DwarfUnit _currentUnit;
        private ulong _sizeOf;
        private DwarfAbbreviation _currentAbbreviation;

        private DiagnosticBag Diagnostics;

        private bool Is64BitEncoding;

        private bool Is64BitAddress;





    }
}