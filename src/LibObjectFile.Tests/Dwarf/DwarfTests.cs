// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Diagnostics;
using LibObjectFile.Dwarf;
using LibObjectFile.Elf;

namespace LibObjectFile.Tests.Dwarf;

[TestClass]
public class DwarfTests
{
    [DataTestMethod]
    [DataRow(0UL)]
    [DataRow(1UL)]
    [DataRow(50UL)]
    [DataRow(0x7fUL)]
    [DataRow(0x80UL)]
    [DataRow(0x81UL)]
    [DataRow(0x12345UL)]
    [DataRow(2147483647UL)] // int.MaxValue
    [DataRow(4294967295UL)] // uint.MaxValue
    [DataRow(ulong.MaxValue)]
    public void TestLEB128(ulong value)
    {
        var stream = new MemoryStream();

        stream.WriteULEB128(value);

        Assert.AreEqual((uint)stream.Position, DwarfHelper.SizeOfULEB128(value));

        stream.Position = 0;
        var readbackValue = stream.ReadULEB128();

        Assert.AreEqual(value, readbackValue);
    }

    [DataTestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(50L)]
    [DataRow(0x7fL)]
    [DataRow(0x80L)]
    [DataRow(0x81L)]
    [DataRow(0x12345L)]
    [DataRow(2147483647L)] // int.MaxValue
    [DataRow(4294967295L)] // uint.MaxValue
    [DataRow(long.MinValue)]
    [DataRow(long.MaxValue)]
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


    [TestMethod]
    public void TestDebugLineHelloWorld()
    {
        var cppName = "helloworld";
        var cppExe = $"{cppName}_debug";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -gdwarf-4 -o {cppExe}");

        ElfFile elf;
        using (var inStream = File.OpenRead(cppExe))
        {
            Console.WriteLine($"ReadBack from {cppExe}");
            elf = ElfFile.Read(inStream);
            elf.Print(Console.Out);
        }

        var elfContext = new DwarfElfContext(elf);
        var inputContext = new DwarfReaderContext(elfContext);
        inputContext.DebugLinePrinter = Console.Out;
        var dwarf = DwarfFile.Read(inputContext);

        inputContext.DebugLineStream!.Position = 0;

        var copyInputDebugLineStream = new MemoryStream();
        inputContext.DebugLineStream.CopyTo(copyInputDebugLineStream);
        inputContext.DebugLineStream.Position = 0;

        var outputContext = new DwarfWriterContext
        {
            IsLittleEndian = inputContext.IsLittleEndian,
            EnableRelocation = false,
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
        ByteArrayAssert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
    }

    [TestMethod]
    public void TestDebugLineLibMultipleObjs()
    {
        var cppName = "lib";
        var libShared = $"{cppName}_debug.so";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}_a.cpp {cppName}_b.cpp -gdwarf-4 -shared -o {libShared}");

        ElfFile elf;
        using (var inStream = File.OpenRead(libShared))
        {
            Console.WriteLine($"ReadBack from {libShared}");
            elf = ElfFile.Read(inStream);
            elf.Print(Console.Out);
        }

        var elfContext = new DwarfElfContext(elf);
        var inputContext = new DwarfReaderContext(elfContext);
        inputContext.DebugLinePrinter = Console.Out;
        var dwarf = DwarfFile.Read(inputContext);

        inputContext.DebugLineStream!.Position = 0;

        var copyInputDebugLineStream = new MemoryStream();
        inputContext.DebugLineStream.CopyTo(copyInputDebugLineStream);
        inputContext.DebugLineStream.Position = 0;

        var outputContext = new DwarfWriterContext
        {
            IsLittleEndian = inputContext.IsLittleEndian,
            EnableRelocation = false,
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
        ByteArrayAssert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
    }

    [TestMethod]
    public void TestDebugLineSmall()
    {
        var cppName = "small";
        var cppObj = $"{cppName}_debug.o";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -gdwarf-4 -c -o {cppObj}");
        ElfFile elf;
        using (var inStream = File.OpenRead(cppObj))
        {
            Console.WriteLine($"ReadBack from {cppObj}");
            elf = ElfFile.Read(inStream);
            elf.Print(Console.Out);
        }

        var elfContext = new DwarfElfContext(elf);
        var inputContext = new DwarfReaderContext(elfContext);
        inputContext.DebugLinePrinter = Console.Out;
        var dwarf = DwarfFile.Read(inputContext);

        inputContext.DebugLineStream!.Position = 0;
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
        ByteArrayAssert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
    }



    [TestMethod]
    public void TestDebugLineMultipleFunctions()
    {
        var cppName = "multiple_functions";
        var cppObj = $"{cppName}_debug.o";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -gdwarf-4 -c -o {cppObj}");

        ElfFile elf;
        using (var inStream = File.OpenRead(cppObj))
        {
            Console.WriteLine($"ReadBack from {cppObj}");
            elf = ElfFile.Read(inStream);
            elf.Print(Console.Out);
        }

        var elfContext = new DwarfElfContext(elf);
        var inputContext = new DwarfReaderContext(elfContext);
        inputContext.DebugLinePrinter = Console.Out;
        var dwarf = DwarfFile.Read(inputContext);

        inputContext.DebugLineStream!.Position = 0;
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
        ByteArrayAssert.AreEqual(inputDebugLineBuffer, outputDebugLineBuffer);
    }


    [TestMethod]
    public void TestDebugInfoSmall()
    {
        var cppName = "small";
        var cppObj = $"{cppName}_debug.o";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -gdwarf-4 -c -o {cppObj}");

        ElfFile elf;
        using (var inStream = File.OpenRead(cppObj))
        {
            elf = ElfFile.Read(inStream);
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


    [TestMethod]
    public void CreateDwarf()
    {
        // Create ELF object
        var elf = new ElfFile(ElfArch.X86_64);

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, new MemoryStream(new byte[0x64]));
        elf.Content.Add(codeSection);
        var stringSection = new ElfStringTable();
        elf.Content.Add(stringSection);
        elf.Content.Add(new ElfSymbolTable() { Link = stringSection });
        elf.Content.Add(new ElfSectionHeaderStringTable());
        elf.Content.Add(new ElfSectionHeaderTable());

        var elfDiagnostics = new DiagnosticBag();
        elf.UpdateLayout(elfDiagnostics);
        Assert.IsFalse(elfDiagnostics.HasErrors);

        // Create DWARF Object
        var dwarfFile = new DwarfFile();

        // Create .debug_line information
        var fileName = new DwarfFileName("check1.cpp")
        {
            Directory = Environment.CurrentDirectory,
        };
        var fileName2 = new DwarfFileName("check2.cpp")
        {
            Directory = Environment.CurrentDirectory,
        };

        // First line table
        for (int i = 0; i < 2; i++)
        {
            var lineTable = new DwarfLineProgramTable();
            dwarfFile.LineSection.LineTables.Add(lineTable);

            lineTable.AddressSize = DwarfAddressSize.Bit64;
            lineTable.FileNames.Add(fileName);
            lineTable.FileNames.Add(fileName2);

            lineTable.LineSequences.Add(new DwarfLineSequence()
                {
                    new DwarfLine()
                    {
                        File = fileName,
                        Address = 0,
                        Column = 1,
                        Line = 1,
                    },
                    new DwarfLine()
                    {
                        File = fileName,
                        Address = 1,
                        Column = 1,
                        Line = 2,
                    }
                }
            );
            // NOTE: doesn't seem to be generated by regular GCC
            // (it seems that only one line sequence is usually used)
            lineTable.LineSequences.Add(new DwarfLineSequence()
                {

                    new DwarfLine()
                    {
                        File = fileName2,
                        Address = 0,
                        Column = 1,
                        Line = 1,
                    },
                }
            );
        }

        // Create .debug_info
        var rootDIE = new DwarfDIECompileUnit()
        {
            Name = fileName.Name,
            LowPC = 0, // 0 relative to base virtual address
            HighPC = (int)codeSection.Size, // default is offset/length after LowPC
            CompDir = fileName.Directory,
            StmtList = dwarfFile.LineSection.LineTables[0],
        };
        var subProgram = new DwarfDIESubprogram()
        {
            Name = "MyFunction",
        };
        rootDIE.Children.Add(subProgram);

        var locationList = new DwarfLocationList();
        var regExpression = new DwarfExpression();
        regExpression.Operations.Add(new DwarfOperation { Kind = DwarfOperationKindEx.Reg0 });
        var regExpression2 = new DwarfExpression();
        regExpression2.Operations.Add(new DwarfOperation { Kind = DwarfOperationKindEx.Reg2 });
        locationList.LocationListEntries.Add(new DwarfLocationListEntry
        {
            Start = 0,
            End = 0x10,
            Expression = regExpression,
        });
        locationList.LocationListEntries.Add(new DwarfLocationListEntry
        {
            Start = 0x10,
            End = 0x20,
            Expression = regExpression2,
        });
        var variable = new DwarfDIEVariable()
        {
            Name = "a",
            Location = locationList,
        };
        dwarfFile.LocationSection.LocationLists.Add(locationList);
        subProgram.Children.Add(variable);

        var cu = new DwarfCompilationUnit()
        {
            AddressSize = DwarfAddressSize.Bit64,
            Root = rootDIE
        };
        dwarfFile.InfoSection.Units.Add(cu);

        // AddressRange table
        dwarfFile.AddressRangeTable.AddressSize = DwarfAddressSize.Bit64;
        dwarfFile.AddressRangeTable.Unit = cu;
        dwarfFile.AddressRangeTable.Ranges.Add(new DwarfAddressRange(0, 0, codeSection.Size));

        // Transfer DWARF To ELF
        var dwarfElfContext = new DwarfElfContext(elf);
        dwarfFile.WriteToElf(dwarfElfContext);

        var outputFileName = "create_dwarf.o";
        using (var output = new FileStream(outputFileName, FileMode.Create))
        {
            elf.Write(output);
        }

        elf.Print(Console.Out);
        Console.WriteLine();
        dwarfFile.AbbreviationTable.Print(Console.Out);
        Console.WriteLine();
        dwarfFile.AddressRangeTable.Print(Console.Out);
        Console.WriteLine();
        dwarfFile.InfoSection.Print(Console.Out);

        Console.WriteLine("ReadBack --debug-dump=rawline");
        var readelf = LinuxUtil.ReadElf(outputFileName, "--debug-dump=rawline").TrimEnd();
        Console.WriteLine(readelf);
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