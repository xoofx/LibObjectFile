// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Dwarf;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests.Dwarf
{
    public class DwarfTests
    {
        [TestCase(0UL)]
        [TestCase(1UL)]
        [TestCase(50UL)]
        [TestCase(0x7fUL)]
        [TestCase(0x80UL)]
        [TestCase(0x81UL)]
        [TestCase(0x12345UL)]
        [TestCase(2147483647UL)] // int.MaxValue
        [TestCase(4294967295UL)] // uint.MaxValue
        [TestCase(ulong.MaxValue)]
        public void TestLEB128(ulong value)
        {
            var stream = new MemoryStream();

            stream.WriteULEB128(value);
            
            Assert.AreEqual((uint)stream.Position, DwarfHelper.SizeOfULEB128(value));

            stream.Position = 0;
            var readbackValue = stream.ReadULEB128();

            Assert.AreEqual(value, readbackValue);
        }

        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(50L)]
        [TestCase(0x7fL)]
        [TestCase(0x80L)]
        [TestCase(0x81L)]
        [TestCase(0x12345L)]
        [TestCase(2147483647L)] // int.MaxValue
        [TestCase(4294967295L)] // uint.MaxValue
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void TestSignedLEB128(long value)
        {
            var stream = new MemoryStream();

            {
                // check positive
                stream.WriteILEB128(value);
                Assert.AreEqual((uint)stream.Position, DwarfHelper.SizeOfILEB128(value));

                stream.Position = 0;
                var readbackValue = stream.ReadSignedLEB128();
                Assert.AreEqual(value, readbackValue);
            }

            {
                stream.Position = 0;
                // Check negative
                value = -value;
                stream.WriteILEB128(value);
                Assert.AreEqual((uint)stream.Position, DwarfHelper.SizeOfILEB128(value));

                stream.Position = 0;
                var readbackValue = stream.ReadSignedLEB128();
                Assert.AreEqual(value, readbackValue);
            }
        }


        [Test]
        public void TestDebugLineHelloWorld()
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

            var elfContext = new DwarfElfContext(elf);
            var inputContext = new DwarfReaderContext(elfContext);
            inputContext.DebugLinePrinter = Console.Out;
            var dwarf = DwarfFile.Read(inputContext);

            inputContext.DebugLineStream.Position = 0;

            var copyInputDebugLineStream = new MemoryStream();
            inputContext.DebugLineStream.CopyTo(copyInputDebugLineStream);
            inputContext.DebugLineStream.Position = 0;

            var outputContext = new DwarfWriterContext
            {
                IsLittleEndian = inputContext.IsLittleEndian,
                LayoutConfig = { GenerateRelocation = false },
                AddressSize = inputContext.AddressSize,
                DebugLineStream = new MemoryStream()
            };
            dwarf.Write(outputContext);

            Console.WriteLine();
            Console.WriteLine("=====================================================");
            Console.WriteLine("Readback");
            Console.WriteLine("=====================================================");
            Console.WriteLine();

            var reloadContext = new DwarfReaderContext()
            {
                IsLittleEndian = outputContext.IsLittleEndian,
                AddressSize = outputContext.AddressSize,
                DebugLineStream = outputContext.DebugLineStream
            };

            reloadContext.DebugLineStream.Position = 0;
            reloadContext.DebugLineStream = outputContext.DebugLineStream;
            reloadContext.DebugLinePrinter = Console.Out;

            var dwarf2 = DwarfFile.Read(reloadContext);

            var inputDebugLineBuffer = copyInputDebugLineStream.ToArray();
            var outputDebugLineBuffer = ((MemoryStream)reloadContext.DebugLineStream).ToArray();
            Assert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
        }

        [Test]
        public void TestDebugLineSmall()
        {
            var cppName = "small";
            var cppObj = $"{cppName}_debug.o";
            LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -g -c -o {cppObj}");

            ElfObjectFile elf;
            using (var inStream = File.OpenRead(cppObj))
            {
                Console.WriteLine($"ReadBack from {cppObj}");
                elf = ElfObjectFile.Read(inStream);
                elf.Print(Console.Out);
            }

            var elfContext = new DwarfElfContext(elf);
            var inputContext = new DwarfReaderContext(elfContext);
            inputContext.DebugLinePrinter = Console.Out;
            var dwarf = DwarfFile.Read(inputContext);

            inputContext.DebugLineStream.Position = 0;
            var copyInputDebugLineStream = new MemoryStream();
            inputContext.DebugLineStream.CopyTo(copyInputDebugLineStream);
            inputContext.DebugLineStream.Position = 0;

            var outputContext = new DwarfWriterContext
            {
                IsLittleEndian = inputContext.IsLittleEndian,
                AddressSize = inputContext.AddressSize,
                DebugLineStream = new MemoryStream()
            };
            dwarf.Write(outputContext);

            Console.WriteLine();
            Console.WriteLine("=====================================================");
            Console.WriteLine("Readback");
            Console.WriteLine("=====================================================");
            Console.WriteLine();

            var reloadContext = new DwarfReaderContext()
            {
                IsLittleEndian = outputContext.IsLittleEndian,
                AddressSize = outputContext.AddressSize,
                DebugLineStream = outputContext.DebugLineStream
            };

            reloadContext.DebugLineStream.Position = 0;
            reloadContext.DebugLineStream = outputContext.DebugLineStream;
            reloadContext.DebugLinePrinter = Console.Out;

            var dwarf2 = DwarfFile.Read(reloadContext);

            var inputDebugLineBuffer = copyInputDebugLineStream.ToArray();
            var outputDebugLineBuffer = ((MemoryStream)reloadContext.DebugLineStream).ToArray();
            Assert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
        }


        [Test]
        public void TestDebugInfoSmall()
        {
            var cppName = "small";
            var cppObj = $"{cppName}_debug.o";
            LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -g -c -o {cppObj}");

            ElfObjectFile elf;
            using (var inStream = File.OpenRead(cppObj))
            {
                elf = ElfObjectFile.Read(inStream);
                elf.Print(Console.Out);
            }

            var elfContext = new DwarfElfContext(elf);
            var inputContext = new DwarfReaderContext(elfContext);
            var dwarf = DwarfFile.Read(inputContext);

            dwarf.AbbreviationTable.Print(Console.Out);
            dwarf.InfoSection.Print(Console.Out);
            dwarf.AddressRangeTable.Print(Console.Out);

            PrintStreamLength(inputContext);

            Console.WriteLine();
            Console.WriteLine("====================================================================");
            Console.WriteLine("Write Back");
            Console.WriteLine("====================================================================");

            var outputContext = new DwarfWriterContext
            {
                IsLittleEndian = inputContext.IsLittleEndian,
                AddressSize = inputContext.AddressSize,
                DebugAbbrevStream = new MemoryStream(),
                DebugLineStream = new MemoryStream(),
                DebugInfoStream = new MemoryStream(),
                DebugStringStream =  new MemoryStream(),
                DebugAddressRangeStream = new MemoryStream()
            };
            dwarf.Write(outputContext);

            dwarf.AbbreviationTable.Print(Console.Out);
            dwarf.InfoSection.Print(Console.Out);
            dwarf.InfoSection.PrintRelocations(Console.Out);
            dwarf.AddressRangeTable.Print(Console.Out);
            dwarf.AddressRangeTable.PrintRelocations(Console.Out);

            dwarf.WriteToElf(elfContext);

            var cppObj2 = $"{cppName}_debug2.o";
            using (var outStream = new FileStream(cppObj2, FileMode.Create))
            {
                elf.Write(outStream);
            }

            PrintStreamLength(outputContext);
        }

        private static void PrintStreamLength(DwarfReaderWriterContext context)
        {
            if (context.DebugInfoStream != null)
            {
                Console.WriteLine($".debug_info {context.DebugInfoStream.Length}");
            }
            if (context.DebugAbbrevStream != null)
            {
                Console.WriteLine($".debug_abbrev {context.DebugAbbrevStream.Length}");
            }
            if (context.DebugAddressRangeStream != null)
            {
                Console.WriteLine($".debug_aranges {context.DebugAddressRangeStream.Length}");
            }
            if (context.DebugStringStream != null)
            {
                Console.WriteLine($".debug_str {context.DebugStringStream.Length}");
            }
            if (context.DebugLineStream != null)
            {
                Console.WriteLine($".debug_line {context.DebugLineStream.Length}");
            }
        }
    }
}