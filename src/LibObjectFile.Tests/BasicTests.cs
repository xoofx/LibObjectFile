using System;
using System.IO;
using System.Text;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests
{
    public class BasicTests
    {
        [Test]
        public void SimpleCodeSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
            codeStream.Position = 0;

            var codeSection = new ElfCustomSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            elf.AddSection(codeSection);

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

            elf.ProgramHeaders.Add(new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Offset = new ElfSectionOffset(codeSection, 0),
                    VirtualAddress = 0x1000, 
                    PhysicalAddress = 0x1000, 
                    Flags = ElfSegmentFlagsCore.Readable|ElfSegmentFlagsCore.Executable,
                    SizeInFile = 4096,
                    SizeInMemory = 4096,
                    Align = 4096,
            });

            elf.ProgramHeaders.Add(new ElfSegment()
            {
                Type = ElfSegmentTypeCore.Load,
                Offset = new ElfSectionOffset(dataSection, 0),
                VirtualAddress = 0x2000,
                PhysicalAddress = 0x2000,
                Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
                SizeInFile = 1024,
                SizeInMemory = 1024,
                Align = 4096,
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

            elf.ProgramHeaders.Add(
                new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Offset = new ElfSectionOffset(codeSection, 0),
                    VirtualAddress = 0x1000,
                    PhysicalAddress = 0x1000,
                    Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Executable,
                    SizeInFile = 4096,
                    SizeInMemory = 4096,
                    Align = 4096,
                }
            );

            elf.ProgramHeaders.Add(
                new ElfSegment()
                {
                    Type = ElfSegmentTypeCore.Load,
                    Offset = new ElfSectionOffset(dataSection, 0),
                    VirtualAddress = 0x2000,
                    PhysicalAddress = 0x2000,
                    Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable,
                    SizeInFile = 1024,
                    SizeInMemory = 1024,
                    Align = 4096,
                }
            );

            var relocTable = elf.AddSection(
                new ElfRelocationTable
                {
                    Link = symbolSection,
                    TargetSection = codeSection,
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

            AssertReadElf(elf, "test4.elf");
        }

        private static void AssertReadElf(ElfObjectFile elf, string fileName)
        {
            var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, fileName), FileMode.Create);
            elf.Write(stream);

            stream.Flush();
            stream.Close();

            var stringWriter = new StringWriter();
            elf.Print(stringWriter);

            var result = stringWriter.ToString().Replace("\r\n", "\n");
            var readelf = LinuxUtil.ReadElf(fileName);
            Console.WriteLine("=== Expected:");
            Console.WriteLine(readelf);
            Console.WriteLine("=== Result:");
            Console.WriteLine(result);
            Assert.AreEqual(readelf, result);
        }
    }
}