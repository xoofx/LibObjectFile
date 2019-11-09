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
    }
}