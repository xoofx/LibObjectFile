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

            var codeSection = new ElfStreamSection().ConfigureAs(ElfSectionSpecialType.Text);
            var codeStream = new MemoryStream();
            codeStream.Write(Encoding.UTF8.GetBytes("This is a text"));
            codeStream.Position = 0;
            codeSection.Stream = codeStream;

            elf.AddSection(codeSection);

            var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "test.elf"), FileMode.Create);

            elf.Write(stream);

            stream.Flush();
            stream.Close();
        }
    }
}