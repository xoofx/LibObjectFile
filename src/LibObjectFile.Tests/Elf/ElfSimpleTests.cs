// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests.Elf
{
    public class ElfSimpleTests : ElfTestBase
    {
        [Test]
        public void SimpleEmptyWithDefaultSections()
        {
            var elf = new ElfObjectFile();
            AssertReadElf(elf, "empty_default.elf");
        }

        [Test]
        public void SimpleEmpty()
        {
            var elf = new ElfObjectFile();
            for (int i = elf.Sections.Count - 1; i >= 0; i--)
            {
                elf.RemoveSectionAt(i);
            }
            AssertReadElf(elf, "empty.elf");
        }

        [Test]
        public void SimpleCodeSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
            codeStream.Position = 0;

            var codeSection = new ElfCustomSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            elf.AddSection(codeSection);
            elf.AddSection(new ElfSectionHeaderStringTable());

            AssertReadElf(elf, "test.elf");
        }
        
        [Test]
        public void SimpleCodeSectionAndSymbolSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
            codeStream.Position = 0;

            var codeSection = new ElfCustomSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            elf.AddSection(codeSection);

            var stringSection = new ElfStringTable();
            elf.AddSection(stringSection);
            
            var symbolSection = new ElfSymbolTable()
            {
                Link = stringSection,

                Entries =
                {
                    new ElfSymbol()
                    {
                        Name = "local_symbol",
                        Bind = ElfSymbolBind.Local,
                        Section = codeSection,
                        Size = 16,
                        Type = ElfSymbolType.Function,
                        Visibility = ElfSymbolVisibility.Protected,
                        Value = 0x7896
                    },
                    new ElfSymbol()
                    {
                        Name = "GlobalSymbol",
                        Bind = ElfSymbolBind.Global,
                        Section = codeSection,
                        Size = 4,
                        Type = ElfSymbolType.Function,
                        Value = 0x12345
                    }
                }
            };
            elf.AddSection(symbolSection);
            elf.AddSection(new ElfSectionHeaderStringTable());

            AssertReadElf(elf, "test2.elf");
        }

        [Test]
        public void SimpleProgramHeaderAndCodeSectionAndSymbolSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(new byte[4096]);
            
            var codeSection = elf.AddSection(
                new ElfCustomSection(codeStream)
                {
                    VirtualAddress = 0x1000,
                    Alignment = 4096
                }.ConfigureAs(ElfSectionSpecialType.Text)
            );


            var dataStream = new MemoryStream();
            dataStream.Write(new byte[1024]);

            var dataSection = elf.AddSection(
                new ElfCustomSection(dataStream)
                {
                    VirtualAddress = 0x2000,
                    Alignment = 4096
                }.ConfigureAs(ElfSectionSpecialType.ReadOnlyData)
            );

            var stringSection = elf.AddSection(new ElfStringTable());

            var symbolSection = elf.AddSection(
                new ElfSymbolTable()
                {
                    Link = stringSection,

                    Entries =
                    {
                        new ElfSymbol()
                        {
                            Name = "local_symbol",
                            Bind = ElfSymbolBind.Local,
                            Section = codeSection,
                            Size = 16,
                            Type = ElfSymbolType.Function,
                            Visibility = ElfSymbolVisibility.Protected,
                            Value = 0x7896
                        },
                        new ElfSymbol()
                        {
                            Name = "GlobalSymbol",
                            Bind = ElfSymbolBind.Global,
                            Section = codeSection,
                            Size = 4,
                            Type = ElfSymbolType.Function,
                            Value = 0x12345
                        }
                    }
                }
            );
            elf.AddSection(new ElfSectionHeaderStringTable());

            elf.AddSegment(new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Range = codeSection,
                    VirtualAddress = 0x1000, 
                    PhysicalAddress = 0x1000, 
                    Flags = ElfSegmentFlagsCore.Readable|ElfSegmentFlagsCore.Executable,
                    Size = 4096,
                    SizeInMemory = 4096,
                    Alignment = 4096,
            });

            elf.AddSegment(new ElfSegment()
            {
                Type = ElfSegmentTypeCore.Load,
                Range = dataSection,
                VirtualAddress = 0x2000,
                PhysicalAddress = 0x2000,
                Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
                Size = 1024,
                SizeInMemory = 1024,
                Alignment = 4096,
            });

            AssertReadElf(elf, "test3.elf");
        }


        [Test]
        public void SimpleProgramHeaderAndCodeSectionAndSymbolSectionAndRelocation()
        {
            var arch = ElfArch.X86_64;

            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(new byte[4096]);

            var codeSection = elf.AddSection(
                new ElfCustomSection(codeStream)
                {
                    VirtualAddress = 0x1000,
                    Alignment = 4096
                }.ConfigureAs(ElfSectionSpecialType.Text)
            );


            var dataStream = new MemoryStream();
            dataStream.Write(new byte[1024]);

            var dataSection = elf.AddSection(
                new ElfCustomSection(dataStream)
                {
                    VirtualAddress = 0x2000,
                    Alignment = 4096
                }.ConfigureAs(ElfSectionSpecialType.ReadOnlyData)
            );

            var stringSection = elf.AddSection(new ElfStringTable());

            var symbolSection = elf.AddSection(
                new ElfSymbolTable()
                {
                    Link = stringSection,

                    Entries =
                    {
                        new ElfSymbol()
                        {
                            Name = "local_symbol",
                            Bind = ElfSymbolBind.Local,
                            Section = codeSection,
                            Size = 16,
                            Type = ElfSymbolType.Function,
                            Visibility = ElfSymbolVisibility.Protected,
                            Value = 0x7896
                        },
                        new ElfSymbol()
                        {
                            Name = "GlobalSymbol",
                            Bind = ElfSymbolBind.Global,
                            Section = codeSection,
                            Size = 4,
                            Type = ElfSymbolType.Function,
                            Value = 0x12345
                        }
                    }
                }
            );

            elf.AddSegment(
                new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Range = codeSection,
                    VirtualAddress = 0x1000,
                    PhysicalAddress = 0x1000,
                    Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Executable,
                    Size = 4096,
                    SizeInMemory = 4096,
                    Alignment = 4096,
                }
            );

            elf.AddSegment(
                new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Range = dataSection,
                    VirtualAddress = 0x2000,
                    PhysicalAddress = 0x2000,
                    Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
                    Size = 1024,
                    SizeInMemory = 1024,
                    Alignment = 4096,
                }
            );

            var relocTable = elf.AddSection(
                new ElfRelocationTable
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
                }
            );

            elf.AddSection(new ElfSectionHeaderStringTable());

            AssertReadElf(elf, "test4.elf");
        }


        [Test]
        public void TestHelloWorld()
        {
            var cppName = "helloworld";
            LinuxUtil.RunLinuxExe("gcc", $"{cppName}.cpp -o {cppName}");

            ElfObjectFile elf;
            using (var inStream = File.OpenRead(cppName))
            {
                Console.WriteLine($"ReadBack from {cppName}");
                elf = ElfObjectFile.Read(inStream);
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
    }
}