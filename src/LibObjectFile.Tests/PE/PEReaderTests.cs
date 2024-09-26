// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LibObjectFile.PE;

namespace LibObjectFile.Tests.PE;

[TestClass]
public class PEReaderTests
{
    [TestMethod]
    [DataRow("NativeConsoleWin64.exe")]
    [DataRow("NativeConsole2Win64.exe")]
    [DataRow("NativeLibraryWin64.dll")]

    public void TestPrinter(string name)
    {
        var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "PE", name));
        var peImage = PEFile.Read(stream);
        peImage.Print(Console.Out);
    }
}