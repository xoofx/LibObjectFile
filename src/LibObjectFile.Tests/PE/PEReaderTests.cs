// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [DataRow("RawNativeConsoleWin64.exe")]

    public async Task TestPrinter(string name)
    {
        var sourceFile = Path.Combine(AppContext.BaseDirectory, "PE", name);
        await using var stream = File.OpenRead(sourceFile);
        var peImage = PEFile.Read(stream, new() { EnableStackTrace = true });
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
        byte[] outputBuffer = output.ToArray();

        // Fake an error
        //outputBuffer[250] = 0x44;

        //await Verifier.Verify(outputBuffer, sourceFile  sourceFile).
        await File.WriteAllBytesAsync($"{sourceFile}.bak", outputBuffer);

        // Compare the input and output buffer
        ByteArrayAssert.AreEqual(inputBuffer, outputBuffer, $"Invalid roundtrip for `{name}`");
    }

    [TestMethod]
    public void TestCreatePE()
    {
        var pe = new PEFile();
        
        // ***************************************************************************
        // Code section
        // ***************************************************************************
        var codeSection = pe.AddSection(PESectionName.Text, 0x1000);
        var streamCode = new PEStreamSectionData();
        
        streamCode.Stream.Write([
            // SUB RSP, 0x28
            0x48, 0x83, 0xEC, 0x28,
            // MOV ECX, 0x9C
            0xB9, 0x9C, 0x00, 0x00, 0x00,
            // CALL ExitProcess (CALL [RIP + 0xFF1])  
            0xFF, 0x15, 0xF1, 0x0F, 0x00, 0x00,
            // INT3
            0xCC
        ]);

        codeSection.Content.Add(streamCode);
        
        // ***************************************************************************
        // Data section
        // ***************************************************************************
        var dataSection = pe.AddSection(PESectionName.RData, 0x2000);

        var streamData = new PEStreamSectionData();
        var kernelName = streamData.WriteAsciiString("KERNEL32.DLL");
        var exitProcessFunction = streamData.WriteHintName(new(0x178, "ExitProcess"));

        // PEImportAddressTableDirectory comes first, it is referenced by the RIP + 0xFF1, first address being ExitProcess
        var peImportAddressTable = new PEImportAddressTable()
        {
            exitProcessFunction
        };
        var iatDirectory = new PEImportAddressTableDirectory()
        {
            peImportAddressTable
        };
        
        var peImportLookupTable = new PEImportLookupTable()
        {
            exitProcessFunction
        };

        var importDirectory = new PEImportDirectory()
        {
            Entries =
            {
                new PEImportDirectoryEntry(kernelName, peImportAddressTable, peImportLookupTable)
            }
        };

        // Layout of the data section
        dataSection.Content.Add(iatDirectory);
        dataSection.Content.Add(peImportLookupTable);
        dataSection.Content.Add(importDirectory);
        dataSection.Content.Add(streamData);

        // ***************************************************************************
        // Optional Header
        // ***************************************************************************
        pe.OptionalHeader.AddressOfEntryPoint = new(streamCode, 0);
        pe.OptionalHeader.BaseOfCode = codeSection;

        // ***************************************************************************
        // Write the PE to a file
        // ***************************************************************************
        var output = new MemoryStream();
        pe.Write(output);
        output.Position = 0;

        var sourceFile = Path.Combine(AppContext.BaseDirectory, "PE", "generated_win64.exe");
        File.WriteAllBytes(sourceFile, output.ToArray());
    }

    [DataTestMethod]
    [DynamicData(nameof(GetWindowsExeAndDlls), DynamicDataSourceType.Method)]
    public async Task TestWindows(string sourceFile)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("This test can only run on Windows");
            return;
        }

        TestContext.WriteLine($"Testing {sourceFile}");
        await using var stream = File.OpenRead(sourceFile);
        var peImage = PEFile.Read(stream, new() { EnableStackTrace = true });

        if (peImage.CoffHeader.PointerToSymbolTable != 0)
        {
            Assert.Inconclusive($"The file {sourceFile} contains a non supported symbol table");
            return;
        }

        var sizeOfInitializedData = peImage.OptionalHeader.SizeOfInitializedData;

        // Read in input as raw bytes
        stream.Position = 0;
        var inputBuffer = new byte[stream.Length];
        stream.ReadExactly(inputBuffer);

        peImage.UpdateLayout(new DiagnosticBag());

        var newSizeOfInitializedData = peImage.OptionalHeader.SizeOfInitializedData;

        if (newSizeOfInitializedData != sizeOfInitializedData)
        {
            TestContext.WriteLine($"SizeOfInitializedData changed from {sizeOfInitializedData} to {newSizeOfInitializedData}. Trying to reuse old size");
            peImage.ForceSizeOfInitializedData = sizeOfInitializedData;
        }
        
        // Write the PE back to a byte buffer
        var output = new MemoryStream();
        peImage.Write(output);
        output.Position = 0;
        var outputBuffer = output.ToArray();

        if (!inputBuffer.AsSpan().SequenceEqual(outputBuffer))
        {
            // Uncomment the following code to save the output file to compare it with the original file
            //{
            //    var dir = Path.Combine(AppContext.BaseDirectory, "Errors");
            //    Directory.CreateDirectory(dir);

            //    var sourceFileName = Path.GetFileName(sourceFile);
            //    var outputFileName = Path.Combine(dir, $"{sourceFileName}.new");

            //    await File.WriteAllBytesAsync(outputFileName, outputBuffer);
            //}

            ByteArrayAssert.AreEqual(inputBuffer, outputBuffer, $"Invalid roundtrip for `{sourceFile}`");
        }
    }

    public static IEnumerable<object[]> GetWindowsExeAndDlls()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield return [""];
        }
        else
        {

            foreach (var file in Directory.EnumerateFiles(Environment.SystemDirectory, "*.exe", SearchOption.TopDirectoryOnly))
            {
                yield return [file];
            }

            foreach (var file in Directory.EnumerateFiles(Environment.SystemDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                yield return [file];
            }
        }
    }
    
    [TestMethod]
    [Ignore("PEFile does not support PE files that are folding the PE header into the DosHeader")]
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