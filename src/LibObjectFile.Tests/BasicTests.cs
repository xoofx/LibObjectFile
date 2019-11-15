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
                    Align = 4096,
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
                    Align = 4096,
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
                    Align = 4096,
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

        private static void AssertReadElf(ElfObjectFile elf, string fileName, bool writeFile = true, string context = null)
        {
            if (writeFile)
            {
                using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, fileName), FileMode.Create))
                {
                    elf.Write(stream);
                    stream.Flush();
                }
            }

            var stringWriter = new StringWriter();
            elf.Print(stringWriter);

            var result = stringWriter.ToString().Replace("\r\n", "\n");
            var readelf = LinuxUtil.ReadElf(fileName);
            Console.WriteLine("=== Expected:");
            Console.WriteLine(readelf);
            Console.WriteLine("=== Result:");
            Console.WriteLine(result);
            if (context != null)
            {
                Assert.AreEqual(readelf, result, context);
            }
            else
            {
                Assert.AreEqual(readelf, result);
            }
        }


        private static void AssertReadElf(ElfObjectFile elf, string fileName)
        {
            AssertReadElfInternal(elf, fileName);
            AssertReadback(elf, fileName);
            AssertLsbMsb(elf, fileName);
        }

        private static void AssertReadElfInternal(ElfObjectFile elf, string fileName, bool writeFile = true, string context = null)
        {
            if (writeFile)
            {
                using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, fileName), FileMode.Create))
                {
                    elf.Write(stream);
                    stream.Flush();
                }
            }

            var stringWriter = new StringWriter();
            elf.Print(stringWriter);

            var result = stringWriter.ToString().Replace("\r\n", "\n");
            var readelf = LinuxUtil.ReadElf(fileName);
            Console.WriteLine("=== Expected:");
            Console.WriteLine(readelf);
            Console.WriteLine("=== Result:");
            Console.WriteLine(result);
            if (context != null)
            {
                Assert.AreEqual(readelf, result, context);
            }
            else
            {
                Assert.AreEqual(readelf, result);
            }
        }


        [Test]
        public void TestHelloWorld()
        {
            LinuxUtil.RunLinuxExe("gcc", "helloworld.cpp -o helloworld");

            using (var stream = File.OpenRead("helloworld"))
            {
                var elf = ElfObjectFile.Read(stream);
                elf.Print(Console.Out);

                using (var outstream = File.OpenWrite("helloworld_copy"))
                {
                    elf.Write(outstream);
                }
            }
        }


        private static void AssertReadback(ElfObjectFile elf, string fileName)
        {
            ElfObjectFile newObjectFile;

            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                newObjectFile = ElfObjectFile.Read(stream);
            }

            Console.WriteLine();
            Console.WriteLine("=============================================================================");
            Console.WriteLine("readback");
            Console.WriteLine("=============================================================================");
            Console.WriteLine();

            AssertReadElfInternal(newObjectFile, fileName, false, $"Unexpected error while reading back {fileName}");

            var originalBuffer = File.ReadAllBytes(filePath);
            var memoryStream = new MemoryStream();
            newObjectFile.Write(memoryStream);
            var newBuffer = memoryStream.ToArray();

            Assert.AreEqual(originalBuffer, newBuffer, "Invalid binary diff between write -> (original) -> read -> write -> (new)");
        }

        private static void AssertLsbMsb(ElfObjectFile elf, string fileName)
        {
            Console.WriteLine();
            Console.WriteLine("*****************************************************************************");
            Console.WriteLine("LSB to MSB");
            Console.WriteLine("*****************************************************************************");
            Console.WriteLine();

            elf.Encoding = ElfEncoding.Msb;
            var newFileName = Path.GetFileNameWithoutExtension(fileName) + "_msb.elf";
            AssertReadElfInternal(elf, newFileName);
            AssertReadback(elf, newFileName);
        }
    }
}