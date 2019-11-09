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

            var codeSection = new ElfStreamSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            elf.AddSection(codeSection);

            var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "test.elf"), FileMode.Create);
            elf.Write(stream);

            stream.Flush();
            stream.Close();
        }



        [Test]
        public void SimpleCodeSectionAndSymbolSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
            codeStream.Position = 0;

            var codeSection = new ElfStreamSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            elf.AddSection(codeSection);

            var stringSection = new ElfStringTableSection().ConfigureAs(ElfSectionSpecialType.StringTable);
            elf.AddSection(stringSection);

            var symbolSection = new ElfSymbolTableSection().ConfigureAs(ElfSectionSpecialType.SymbolTable);
            elf.AddSection(symbolSection);
            symbolSection.Link = stringSection;

            symbolSection.Entries.Add(new ElfSymbolTableEntry()
            {
                Name = "local_symbol",
                Bind = ElfSymbolBind.Local,
                Section = codeSection,
                Size = 16,
                Type = ElfSymbolType.Function,
                Visibility = ElfSymbolVisibility.Protected,
                Value = 0x7896
            });

            symbolSection.Entries.Add(new ElfSymbolTableEntry()
            {
                Name = "GlobalSymbol",
                Bind = ElfSymbolBind.Global,
                Section = codeSection,
                Size = 4,
                Type = ElfSymbolType.Function,
                Value = 0x12345
            });
            
            var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "test2.elf"), FileMode.Create);
            elf.Write(stream);

            stream.Flush();
            stream.Close();
        }


        [Test]
        public void SimpleProgramHeaderAndCodeSectionAndSymbolSection()
        {
            var elf = new ElfObjectFile();

            var codeStream = new MemoryStream();
            codeStream.Write(new byte[4096]);
            var codeSection = new ElfStreamSection(codeStream).ConfigureAs(ElfSectionSpecialType.Text);
            codeSection.VirtualAddress = 0x1000;
            codeSection.Alignment = 4096;
            elf.AddSection(codeSection);

            var dataStream = new MemoryStream();
            dataStream.Write(new byte[1024]);
            var dataSection = new ElfStreamSection(dataStream).ConfigureAs(ElfSectionSpecialType.ReadOnlyData);
            dataSection.VirtualAddress = 0x2000;
            dataSection.Alignment = 4096;
            elf.AddSection(dataSection);

            var stringSection = new ElfStringTableSection().ConfigureAs(ElfSectionSpecialType.StringTable);
            elf.AddSection(stringSection);

            var symbolSection = new ElfSymbolTableSection().ConfigureAs(ElfSectionSpecialType.SymbolTable);
            elf.AddSection(symbolSection);
            symbolSection.Link = stringSection;

            symbolSection.Entries.Add(new ElfSymbolTableEntry()
            {
                Name = "local_symbol",
                Bind = ElfSymbolBind.Local,
                Section = codeSection,
                Size = 16,
                Type = ElfSymbolType.Function,
                Visibility = ElfSymbolVisibility.Protected,
                Value = 0x7896
            });

            symbolSection.Entries.Add(new ElfSymbolTableEntry()
            {
                Name = "GlobalSymbol",
                Bind = ElfSymbolBind.Global,
                Section = codeSection,
                Size = 4,
                Type = ElfSymbolType.Function,
                Value = 0x12345
            });


            elf.ProgramHeaders.Add(new ElfProgramHeader()
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

            elf.ProgramHeaders.Add(new ElfProgramHeader()
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

            var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "test3.elf"), FileMode.Create);
            elf.Write(stream);

            stream.Flush();
            stream.Close();
        }
    }
}