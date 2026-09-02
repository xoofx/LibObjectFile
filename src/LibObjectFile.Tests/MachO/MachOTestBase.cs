// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.MachO;
using VerifyMSTest;

namespace LibObjectFile.Tests.MachO;

public abstract class MachOTestBase : VerifyBase
{
    protected static string GetFile(string name) => Path.Combine(AppContext.BaseDirectory, "MachO", name);

    protected static MachOFile LoadMachO(string name) => MachOFile.Read(new MemoryStream(File.ReadAllBytes(GetFile(name))));
}
