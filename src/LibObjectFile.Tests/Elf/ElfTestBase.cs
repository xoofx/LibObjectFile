using System;
using System.IO;
using LibObjectFile.Elf;
using NUnit.Framework;

namespace LibObjectFile.Tests.Elf
{
    public abstract class ElfTestBase
    {
        protected static void AssertReadElf(ElfObjectFile elf, string fileName)
        {
            AssertReadElfInternal(elf, fileName);
            AssertReadBack(elf, fileName);
            AssertLsbMsb(elf, fileName);
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
        
        private static void AssertReadBack(ElfObjectFile elf, string fileName)
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
            AssertReadBack(elf, newFileName);
        }
    }
}