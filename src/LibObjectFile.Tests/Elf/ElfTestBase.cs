// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using LibObjectFile.Elf;
using VerifyMSTest;
using VerifyTests;

namespace LibObjectFile.Tests.Elf;

public abstract class ElfTestBase : VerifyBase
{
    protected async Task AssertReadElf(ElfFile elf, string fileName)
    {
        await VerifyElf(elf, fileName);

        {
            var originalStream = new MemoryStream();
            elf.Write(originalStream);
            

            elf.Encoding = ElfEncoding.Msb;
            var stream = new MemoryStream();
            elf.Write(stream);
            stream.Position = 0;

            var msbElf = ElfFile.Read(stream);
            msbElf.Encoding = ElfEncoding.Lsb;
            stream.SetLength(0);
            msbElf.Write(stream);
            var newData = stream.ToArray();

            ByteArrayAssert.AreEqual(originalStream.ToArray(), newData, "Invalid binary diff between LSB/MSB write -> read -> write");
        }
    }

    protected async Task VerifyElf(ElfFile elf)
    {
        var writer = new StringWriter();
        elf.Print(writer);
        var text = writer.ToString();

        await Verifier.Verify(text);
    }

    protected async Task VerifyElf(ElfFile elf, string name)
    {
        var writer = new StringWriter();
        elf.Print(writer);
        var text = writer.ToString();

        await Verifier.Verify(text).UseParameters(name);
    }

    protected async Task LoadAndVerifyElf(string name)
    {
        var elf = LoadElf(name, out var originalBinary);
        var writer = new StringWriter();
        elf.Print(writer);
        var text = writer.ToString();

        await Verifier.Verify(text).UseParameters(name);

        var memoryStream = new MemoryStream();
        elf.Write(memoryStream);
        var newBinary = memoryStream.ToArray();

        ByteArrayAssert.AreEqual(originalBinary, newBinary, "Invalid binary diff between write -> read -> write");
    }

    protected ElfFile LoadElf(string name)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Elf", name);
        using var stream = File.OpenRead(file);
        return ElfFile.Read(stream);
    }

    protected ElfFile LoadElf(string name, out byte[] originalBinary)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Elf", name);
        originalBinary = File.ReadAllBytes(file);
        using var stream = File.OpenRead(file);
        return ElfFile.Read(stream);
    }
}