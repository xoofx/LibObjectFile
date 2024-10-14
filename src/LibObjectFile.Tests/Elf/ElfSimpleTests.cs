// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.Elf;

namespace LibObjectFile.Tests.Elf;

[TestClass]
public class ElfSimpleTests : ElfTestBase
{
    public TestContext TestContext { get; set; }

    [DataTestMethod]
    [DynamicData(nameof(GetLinuxBins), DynamicDataSourceType.Method)]
    public void TestLinuxFile(string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            Assert.Inconclusive("This test can only run on Linux or on Windows with WSL");
            return;
        }

        using var stream = File.OpenRead(file);
        if (!ElfFile.IsElf(stream)) return;

        var elf = ElfFile.Read(stream);
        stream.Position = 0;
        var elfOriginal = ElfFile.Read(stream); // elfOriginal is not written back, so it's layout is not updated

        var originalBuffer = File.ReadAllBytes(file);

        var copyStream = new MemoryStream();
        elf.Write(copyStream);
        
        for (var i = 0; i < elfOriginal.Content.Count; i++)
        {
            var content = elf.Content[i];
            var contentOriginal = elfOriginal.Content[i];
            Assert.AreEqual(contentOriginal.Position, content.Position, $"Invalid position for content {content}");
            Assert.AreEqual(contentOriginal.Size, content.Size, $"Invalid size for content {content}");
        }
        
        for (var i = 0; i < elfOriginal.Segments.Count; i++)
        {
            var segment = elf.Segments[i];
            var segmentOriginal = elfOriginal.Segments[i];
            Assert.AreEqual(segmentOriginal.Position, segment.Position, $"Invalid position for segment {segment}");
            Assert.AreEqual(segmentOriginal.Size, segment.Size, $"Invalid size for segment {segment}");
        }
        
        ByteArrayAssert.AreEqual(originalBuffer, copyStream.ToArray());
    }

    public static IEnumerable<object[]> GetLinuxBins()
    {
        var wslDirectory = @"\\wsl$\Ubuntu\usr\bin";
        if (OperatingSystem.IsLinux())
        {
            foreach (var file in Directory.EnumerateFiles(@"/usr/bin"))
            {
                yield return new object[] { file };
            }
        }
        else if (OperatingSystem.IsWindows() && Directory.Exists(wslDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(wslDirectory))
            {
                var fileInfo = new FileInfo(file);
                // Skip symbolic links as loading them will fail
                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    yield return new object[] { file };
                }
            }
        }
        else
        {
            yield return new object[] { string.Empty };
        }
    }

    [TestMethod]
    public void TryReadThrows()
    {
        static void CheckInvalidLib(TestContext testContext, bool isReadOnly)
        {
            testContext.WriteLine($"TestThrows ReadOnly: {isReadOnly}");
            using var stream = File.OpenRead("TestFiles/cmnlib.b00");
            Assert.IsFalse(ElfFile.TryRead(stream, out var elf, out var diagnostics, new ElfReaderOptions() { ReadOnly = isReadOnly }));
            Assert.IsNotNull(elf);
            foreach (var message in diagnostics.Messages)
            {
                testContext.WriteLine(message.ToString());
            }

            Assert.AreEqual(3, diagnostics.Messages.Count, "Invalid number of error messages found");
            for (int i = 0; i < diagnostics.Messages.Count; i++)
            {
                Assert.AreEqual(DiagnosticId.ELF_ERR_InvalidSegmentRange, diagnostics.Messages[i].Id);
            }
        }

        CheckInvalidLib(this.TestContext, false);
        CheckInvalidLib(this.TestContext, true);
    }

    [TestMethod]
    public void TryReadFailed()
    {
        using var stream = File.OpenRead(typeof(ElfSimpleTests).Assembly.Location);

        Assert.IsFalse(ElfFile.TryRead(stream, out var elfObjectFile, out var diagnostics));
        Assert.IsTrue(diagnostics.HasErrors);
        Assert.AreEqual(1, diagnostics.Messages.Count);
        Assert.AreEqual(DiagnosticId.ELF_ERR_InvalidHeaderMagic, diagnostics.Messages[0].Id);
    }


    [TestMethod]
    public void SimpleEmptyWithDefaultSections()
    {
        var elf = new ElfFile(ElfArch.X86_64);
        elf.Add(new ElfSectionHeaderTable());
        AssertReadElf(elf, "empty_default.elf");
    }

    [TestMethod]
    public void SimpleEmpty()
    {
        var elf = new ElfFile(ElfArch.X86_64);
        for (int i = elf.Content.Count - 1; i >= 1; i--)
        {
            elf.Content.RemoveAt(i);
        }
        AssertReadElf(elf, "empty.elf");
    }

    [TestMethod]
    public void SimpleCodeSection()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        var codeStream = new MemoryStream();
        codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
        codeStream.Position = 0;

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, codeStream);
        elf.Add(codeSection);
        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        AssertReadElf(elf, "test.elf");
    }

    [TestMethod]
    public void TestBss()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        var stream = new MemoryStream();
        stream.Write(new byte[] { 1, 2, 3, 4 });
        stream.Position = 0;

        var codeSection = elf.Add(new ElfStreamSection(ElfSectionSpecialType.Text, stream));
        var bssSection = elf.Add(new ElfStreamSection(ElfSectionSpecialType.Bss)
        {
            FileAlignment = 1024
        });

        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        var diagnostics = new DiagnosticBag();
        elf.UpdateLayout(diagnostics);
        Assert.IsFalse(diagnostics.HasErrors);

        Assert.AreEqual(1024U, bssSection.Position);

        AssertReadElf(elf, "test_bss.elf");
    }

    [TestMethod]
    public void SimpleCodeSectionAndSymbolSection()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        var codeStream = new MemoryStream();
        codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
        codeStream.Position = 0;

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, codeStream);
        elf.Add(codeSection);

        var stringSection = new ElfStringTable();
        elf.Add(stringSection);

        var symbolSection = new ElfSymbolTable()
        {
            Link = stringSection,

            Entries =
            {
                new ElfSymbol()
                {
                    Name = "local_symbol",
                    Bind = ElfSymbolBind.Local,
                    SectionLink = codeSection,
                    Size = 16,
                    Type = ElfSymbolType.Function,
                    Visibility = ElfSymbolVisibility.Protected,
                    Value = 0x7896
                },
                new ElfSymbol()
                {
                    Name = "GlobalSymbol",
                    Bind = ElfSymbolBind.Global,
                    SectionLink = codeSection,
                    Size = 4,
                    Type = ElfSymbolType.Function,
                    Value = 0x12345
                }
            }
        };
        elf.Add(symbolSection);
        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        AssertReadElf(elf, "test2.elf");
    }

    [TestMethod]
    public void SimpleProgramHeaderAndCodeSectionAndSymbolSection()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        var codeStream = new MemoryStream();
        codeStream.Write(new byte[4096]);

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, codeStream)
        {
            VirtualAddress = 0x1000,
            VirtualAddressAlignment = 4096
        };
        elf.Add(codeSection);

        var dataStream = new MemoryStream();
        dataStream.Write(new byte[1024]);

        var dataSection = new ElfStreamSection(ElfSectionSpecialType.ReadOnlyData, dataStream)
        {
            VirtualAddress = 0x2000,
            VirtualAddressAlignment = 4096
        };
        elf.Add(dataSection);

        var stringSection = new ElfStringTable();
        elf.Add(stringSection);

        var symbolSection = new ElfSymbolTable()
        {
            Link = stringSection,

            Entries =
            {
                new ElfSymbol()
                {
                    Name = "local_symbol",
                    Bind = ElfSymbolBind.Local,
                    SectionLink = codeSection,
                    Size = 16,
                    Type = ElfSymbolType.Function,
                    Visibility = ElfSymbolVisibility.Protected,
                    Value = 0x7896
                },
                new ElfSymbol()
                {
                    Name = "GlobalSymbol",
                    Bind = ElfSymbolBind.Global,
                    SectionLink = codeSection,
                    Size = 4,
                    Type = ElfSymbolType.Function,
                    Value = 0x12345
                }
            }
        };
        elf.Add(symbolSection);

        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        elf.Segments.Add(new ElfSegment()
        {
            Type = ElfSegmentTypeCore.Load,
            Range = codeSection,
            VirtualAddress = 0x1000,
            PhysicalAddress = 0x1000,
            Flags = ElfSegmentFlagsCore.Readable|ElfSegmentFlagsCore.Executable,
            Size = 4096,
            SizeInMemory = 4096,
            VirtualAddressAlignment = 4096,
        });

        elf.Segments.Add(new ElfSegment()
        {
            Type = ElfSegmentTypeCore.Load,
            Range = dataSection,
            VirtualAddress = 0x2000,
            PhysicalAddress = 0x2000,
            Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
            Size = 1024,
            SizeInMemory = 1024,
            VirtualAddressAlignment = 4096,
        });

        AssertReadElf(elf, "test3.elf");
    }


    [TestMethod]
    public void SimpleProgramHeaderAndCodeSectionAndSymbolSectionAndRelocation()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        var codeStream = new MemoryStream();
        codeStream.Write(new byte[4096]);

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, codeStream)
        {
            VirtualAddress = 0x1000,
            VirtualAddressAlignment = 4096
        };
        elf.Add(codeSection);

        var dataStream = new MemoryStream();
        dataStream.Write(new byte[1024]);

        var dataSection = new ElfStreamSection(ElfSectionSpecialType.ReadOnlyData, dataStream)
        {
            VirtualAddress = 0x2000,
            VirtualAddressAlignment = 4096
        };
        elf.Add(dataSection);

        var stringSection = new ElfStringTable();
        elf.Add(stringSection);

        var symbolSection = new ElfSymbolTable()
        {
            Link = stringSection,

            Entries =
            {
                new ElfSymbol()
                {
                    Name = "local_symbol",
                    Bind = ElfSymbolBind.Local,
                    SectionLink = codeSection,
                    Size = 16,
                    Type = ElfSymbolType.Function,
                    Visibility = ElfSymbolVisibility.Protected,
                    Value = 0x7896
                },
                new ElfSymbol()
                {
                    Name = "GlobalSymbol",
                    Bind = ElfSymbolBind.Global,
                    SectionLink = codeSection,
                    Size = 4,
                    Type = ElfSymbolType.Function,
                    Value = 0x12345
                }
            }
        };
        elf.Add(symbolSection);

        elf.Segments.Add(
            new ElfSegment()
            {
                Type = ElfSegmentTypeCore.Load,
                Range = codeSection,
                VirtualAddress = 0x1000,
                PhysicalAddress = 0x1000,
                Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Executable,
                Size = 4096,
                SizeInMemory = 4096,
                VirtualAddressAlignment = 4096,
            }
        );

        elf.Segments.Add(
            new ElfSegment()
            {
                Type = ElfSegmentTypeCore.Load,
                Range = dataSection,
                VirtualAddress = 0x2000,
                PhysicalAddress = 0x2000,
                Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
                Size = 1024,
                SizeInMemory = 1024,
                VirtualAddressAlignment = 4096,
            }
        );

        var relocTable = new ElfRelocationTable
        {
            Name = ".rela.text",
            Link = symbolSection,
            Info = codeSection,
            Entries =
            {
                new ElfRelocation()
                {
                    SymbolIndex = 1,
                    Type = ElfRelocationType.R_X86_64_32,
                    Offset = 0
                },
                new ElfRelocation()
                {
                    SymbolIndex = 2,
                    Type = ElfRelocationType.R_X86_64_8,
                    Offset = 0
                }
            }
        };
        elf.Add(relocTable);

        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        AssertReadElf(elf, "test4.elf");
    }


    [TestMethod]
    public void TestHelloWorld()
    {
        var cppName = "helloworld";
        LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -o {cppName}");

        ElfFile elf;
        using (var inStream = File.OpenRead(cppName))
        {
            Console.WriteLine($"ReadBack from {cppName}");
            elf = ElfFile.Read(inStream);
            elf.Print(Console.Out);
        }

        using (var outStream = File.OpenWrite($"{cppName}_copy"))
        {
            elf.Write(outStream);
            outStream.Flush();
        }

        var expected = LinuxUtil.ReadElf(cppName);
        var result = LinuxUtil.ReadElf($"{cppName}_copy");
        if (expected != result)
        {
            Console.WriteLine("=== Result:");
            Console.WriteLine(result);

            Console.WriteLine("=== Expected:");
            Console.WriteLine(expected);

            Assert.AreEqual(expected, result);
        }
    }

    [TestMethod]
    public void TestAlignedSection()
    {
        var elf = new ElfFile(ElfArch.X86_64);

        // By default 0x1000
        var codeStream = new MemoryStream();
        codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
        codeStream.Position = 0;

        var codeSection = new ElfStreamSection(ElfSectionSpecialType.Text, codeStream)
        {
            FileAlignment = 0x1000,
        };
        elf.Add(codeSection);

        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        var diagnostics = elf.Verify();
        Assert.IsFalse(diagnostics.HasErrors);

        elf.UpdateLayout(diagnostics);
        Assert.IsFalse(diagnostics.HasErrors);

        elf.Print(Console.Out);

        Assert.AreEqual(0x1000ul, codeSection.Position, "Invalid alignment");
    }

    [TestMethod]
    public void TestManySections()
    {
        var elf = new ElfFile(ElfArch.X86_64);
        var stringTable = new ElfStringTable();
        var symbolTable = new ElfSymbolTable { Link = stringTable };

        for (int i = 0; i < ushort.MaxValue; i++)
        {
            var section = new ElfStreamSection(ElfSectionSpecialType.Data) { Name = $".section{i}" };
            elf.Add(section);
            symbolTable.Entries.Add(new ElfSymbol { Type = ElfSymbolType.Section, SectionLink = section });
        }

        elf.Add(stringTable);
        elf.Add(symbolTable);
        elf.Add(new ElfSectionHeaderStringTable());
        elf.Add(new ElfSectionHeaderTable());

        var diagnostics = elf.Verify();
        Assert.IsTrue(diagnostics.HasErrors);
        Assert.AreEqual(DiagnosticId.ELF_ERR_MissingSectionHeaderIndices, diagnostics.Messages[0].Id);

        elf.Add(new ElfSymbolTableSectionHeaderIndices { Link = symbolTable });
        diagnostics = elf.Verify();
        Assert.IsFalse(diagnostics.HasErrors);

        int visibleSectionCount = elf.Sections.Count;

        using (var outStream = File.OpenWrite("manysections"))
        {
            elf.Write(outStream);
            outStream.Flush();
        }

        using (var inStream = File.OpenRead("manysections"))
        {
            elf = ElfFile.Read(inStream);
        }

        Assert.AreEqual(visibleSectionCount, elf.Sections.Count);
        Assert.IsTrue(elf.Content[1] is ElfNullSection);

        for (int i = 0; i < ushort.MaxValue; i++)
        {
            var section = elf.Sections[i + 1];
            Assert.IsInstanceOfType<ElfStreamSection>(section, $"Invalid section at index {i}");
            Assert.AreEqual($".section{i}", section.Name.Value);
        }

        symbolTable = elf.Sections.ToList().OfType<ElfSymbolTable>().FirstOrDefault();
        Assert.IsNotNull(symbolTable);
        for (int i = 0; i < ushort.MaxValue; i++)
        {
            Assert.AreEqual($".section{i}", symbolTable.Entries[i + 1].SectionLink.Section!.Name.Value);
        }
    }

    [TestMethod]
    public void TestReadLibStdc()
    {
        ElfFile elf;
        {
            using var stream = File.OpenRead("libstdc++.so");
            elf = ElfFile.Read(stream);
        }

        var writer = new StringWriter();

        writer.WriteLine($"There are {elf.Sections.Count} section headers, starting at offset 0x{elf.Layout.OffsetOfSectionHeaderTable:x}:");
        ElfPrinter.PrintSectionHeaders(elf, writer);

        var result = writer.ToString().Replace("\r\n", "\n").TrimEnd();
        var readelf = LinuxUtil.ReadElf("libstdc++.so", "-W -S").TrimEnd();

        // Remove the R (retain), that is not present in out implementation.
        readelf = readelf.Replace("R (retain), ", string.Empty);

        if (readelf != result)
        {
            Console.WriteLine("=== Expected:");
            Console.WriteLine(readelf);
            Console.WriteLine("=== Result:");
            Console.WriteLine(result);
            Assert.AreEqual(readelf, result);
        }
    }
}