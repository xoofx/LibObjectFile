// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibObjectFile.Dwarf;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests.Dwarf
{
    public class DwarfTests
    {

        [Test]
        public void SimpleDwarf()
        {
            var cppName = "helloworld";
            var cppExe = $"{cppName}_debug";
            LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -g -o {cppExe}");

            ElfObjectFile elf;
            using (var inStream = File.OpenRead(cppExe))
            {
                Console.WriteLine($"ReadBack from {cppExe}");
                elf = ElfObjectFile.Read(inStream);
                elf.Print(Console.Out);
            }

            var debugInfo = (ElfCustomSection)elf.Sections.FirstOrDefault(x => x.Name.Value == ".debug_info");
            var debugAbbrev = (ElfCustomSection)elf.Sections.FirstOrDefault(x => x.Name.Value == ".debug_abbrev");
            var debugStr = (ElfCustomSection)elf.Sections.FirstOrDefault(x => x.Name.Value == ".debug_str");

            //var dwarfReader = new DwarfReaderWriter(debugInfo.Stream, debugAbbrev.Stream, debugStr.Stream);

            DwarfDebugStringTable stringTable = new DwarfDebugStringTable() {Stream = debugStr.Stream};
            DwarfDebugAbbrevTable abbrevTable = new DwarfDebugAbbrevTable() {Stream = debugAbbrev.Stream};

            var debugInfoReadContext = new DwarfDebugInfoReadContext(stringTable, abbrevTable);

            DwarfDebugInfoSection.TryRead(debugInfo.Stream, elf.Encoding == ElfEncoding.Lsb, debugInfoReadContext, out var debugInfoSection, out var diagnostics);
        }
    }
}