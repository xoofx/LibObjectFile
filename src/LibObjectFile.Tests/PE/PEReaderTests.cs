// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE;
using VerifyMSTest;

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

        var sourceFile = Path.Combine(AppContext.BaseDirectory, "PE", name);
        await using var stream = File.OpenRead(sourceFile);
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

        // Read in input as raw bytes
        stream.Position = 0;
        var inputBuffer = new byte[stream.Length];
        stream.ReadExactly(inputBuffer);

        // Write the PE back to a byte buffer
        var output = new MemoryStream();
        peImage.Write(output);
        output.Position = 0;
        var outputBuffer = output.ToArray();

        //await Verifier.Verify(outputBuffer, sourceFile  sourceFile).
        await File.WriteAllBytesAsync($"{sourceFile}.bak", outputBuffer);

        // Compare the input and output buffer
        CollectionAssert.AreEqual(inputBuffer, outputBuffer);
    }

    [TestMethod]
    public async Task TestTinyExe97Bytes()
    {
        // http://www.phreedom.org/research/tinype/
        // TinyPE: The smallest possible PE file
        // 97 bytes
        byte[] data =
        [
            0x4D, 0x5A, 0x00, 0x00, 0x50, 0x45, 0x00, 0x00, 0x4C, 0x01, 0x01, 0x00, 0x6A, 0x2A, 0x58, 0xC3,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x03, 0x01, 0x0B, 0x01, 0x08, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x68, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02
        ];

        var stream = new MemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Position = 0;

        var peImage = PEFile.Read(stream);
        var writer = new StringWriter();
        peImage.Print(writer);
        var afterReadText = writer.ToString();

        await Verifier.Verify(afterReadText);
    }
}