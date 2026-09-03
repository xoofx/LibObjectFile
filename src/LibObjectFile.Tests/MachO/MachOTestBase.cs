// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using LibObjectFile.MachO;
using VerifyMSTest;
using VerifyTests;

namespace LibObjectFile.Tests.MachO;

public abstract class MachOTestBase : VerifyBase
{
    protected static string GetFile(string name) => Path.Combine(AppContext.BaseDirectory, "MachO", name);

    protected static MachOFile LoadMachO(string name) => MachOFile.Read(new MemoryStream(File.ReadAllBytes(GetFile(name))));

    protected static byte[] WriteToArray(MachOFile file)
    {
        var stream = new MemoryStream();
        file.Write(stream);
        return stream.ToArray();
    }

    protected async Task VerifyMachO(MachOFile file, string name)
    {
        var writer = new StringWriter();
        file.Print(writer);
        await Verifier.Verify(writer.ToString()).UseParameters(name);
    }
}
