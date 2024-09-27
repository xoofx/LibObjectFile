// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE;
using VerifyMSTest;
using VerifyTests;

namespace LibObjectFile.Tests.PE;

[TestClass]
[UsesVerify]
public partial class PEReaderTests
{
    [DataTestMethod]
    [DataRow("NativeConsoleWin64.exe")]
    [DataRow("NativeConsole2Win64.exe")]
    [DataRow("NativeLibraryWin64.dll")]

    public async Task TestPrinter(string name)
    {

        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "PE", name));
        var peImage = PEFile.Read(stream);
        var afterReadWriter = new StringWriter();
        peImage.Print(afterReadWriter);

        var afterReadText = afterReadWriter.ToString();

        await Verifier.Verify(afterReadText).UseParameters(name);

        // Update the layout
        var diagnostics = new DiagnosticBag();
        peImage.UpdateLayout(diagnostics);

        var afterUpdateWriter = new StringWriter();
        peImage.Print(afterUpdateWriter);
        var afterUpdateText = afterUpdateWriter.ToString();

        if (!string.Equals(afterReadText, afterUpdateText, StringComparison.Ordinal))
        {
            TestContext.WriteLine("Error while verifying UpdateLayout");
            await Verifier.Verify(afterUpdateText).UseParameters(name).DisableRequireUniquePrefix();
        }
    }
}