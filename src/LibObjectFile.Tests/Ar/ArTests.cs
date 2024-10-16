// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibObjectFile.Ar;
using LibObjectFile.Diagnostics;
using VerifyTests;

namespace LibObjectFile.Tests.Ar;

[TestClass]
public class ArTests : ArTestBase
{
    [TestMethod]
    public void CheckInvalidHeader()
    {
        // Incorrect magic length
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            Assert.IsFalse(ArArchiveFile.IsAr(stream, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_InvalidMagicLength);
        }
            
        // Correct length, magic invalid
        {
            var stream = new MemoryStream(new byte[]
            {
                (byte)'!',
                (byte)'<',
                (byte)'a',
                (byte)'r',
                (byte)'c',
                (byte)'h',
                (byte)'>',
                (byte)'?',
            });
            Assert.IsFalse(ArArchiveFile.IsAr(stream, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_MagicNotFound);
        }

        // Valid
        {
            var stream = new MemoryStream(new byte[]
            {
                (byte)'!',
                (byte)'<',
                (byte)'a',
                (byte)'r',
                (byte)'c',
                (byte)'h',
                (byte)'>',
                (byte)'\n',
            });
            Assert.IsTrue(ArArchiveFile.IsAr(stream, out var diagnostics));
            ExpectNoDiagnostics(diagnostics);
        }
    }

    [TestMethod] 
    public void CheckInvalidFileEntry()
    {
        // Incorrect entry length
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            stream.Write(new byte[] {(byte)'a', (byte)'b'});
            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_InvalidFileEntryLength);
        }

        // Input invalid non-numeric characters into decimal/octal fields in file entry
        {
            var offsets = new int[]
            {
                ArFile.FieldTimestampOffset,
                ArFile.FieldOwnerIdOffset,
                ArFile.FieldGroupIdOffset,
                ArFile.FieldFileModeOffset,
                ArFile.FieldFileSizeOffset,
                ArFile.FieldEndCharactersOffset
            };
            foreach (var offset in offsets)
            {
                var stream = new MemoryStream();
                stream.Write(ArArchiveFile.Magic);

                var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
                entry.Fill((byte)' ');
                entry[offset] = (byte)'a';
                stream.Write(entry);

                stream.Position = 0;

                Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out var diagnostics));
                ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_InvalidCharacterFoundInFileEntry);
            }
        }

        // Input name with `/`
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
            entry.Fill((byte)' ');
            entry[0] = (byte)'a';
            entry[1] = (byte)'b';
            entry[2] = (byte)'/';
            entry[3] = (byte)'c';

            entry[ArFile.FieldEndCharactersOffset] = (byte)'`';
            entry[ArFile.FieldEndCharactersOffset + 1] = (byte)'\n';

            stream.Write(entry);

            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_InvalidCharacterInFileEntryName);
        }

        // Input length of content
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
            entry.Fill((byte)' ');
            entry[0] = (byte)'a';
            entry[ArFile.FieldFileSizeOffset] = (byte)'2';
            entry[ArFile.FieldEndCharactersOffset] = (byte)'`';
            entry[ArFile.FieldEndCharactersOffset + 1] = (byte)'\n';

            stream.Write(entry);

            var continuePosition = stream.Position;

            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.CMN_ERR_UnexpectedEndOfFile, DiagnosticId.CMN_ERR_UnexpectedEndOfFile);

            stream.Position = continuePosition;

            stream.WriteByte(0);
            stream.WriteByte(1);

            stream.Position = 0;

            // Check that we can actually read the content

            var result = ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out var arFile, out diagnostics);
            ExpectNoDiagnostics(diagnostics);
            Assert.IsTrue(result, $"Error while reading file: {diagnostics}");
            Assert.AreEqual(1, arFile.Files.Count, "Invalid number of file entries found");
            Assert.AreEqual("a", arFile.Files[0].Name, "Invalid name of file entry[0] found");
            Assert.AreEqual(2U, arFile.Files[0].Size, "Invalid size of file entry[0] found");
            Assert.IsInstanceOfType<ArBinaryFile>(arFile.Files[0], "Invalid instance of of file entry[0] ");
                
            var fileStream = ((ArBinaryFile) arFile.Files[0]).Stream!;
            var read = new byte[]
            {
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte()
            };
            ByteArrayAssert.AreEqual(new byte[] { 0, 1}, read, "Invalid content of of file entry[0] ");
                
            Assert.IsNull(arFile.SymbolTable, "Invalid non-null symbol table found");

        }

        // Input length of content
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
            entry.Fill((byte)' ');
            entry[0] = (byte)'a';
            entry[ArFile.FieldFileSizeOffset] = (byte)'1';
            entry[ArFile.FieldEndCharactersOffset] = (byte)'`';
            entry[ArFile.FieldEndCharactersOffset + 1] = (byte)'\n';

            stream.Write(entry);
            stream.WriteByte(0);

            var continuePosition = stream.Position;
            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.CMN_ERR_UnexpectedEndOfFile);

            stream.Position = continuePosition;
            stream.WriteByte(0);
            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_ExpectingNewLineCharacter);

            stream.Position = continuePosition;
            stream.WriteByte((byte)'\n');
            stream.Position = 0;

            Assert.IsTrue(ArArchiveFile.TryRead(stream, ArArchiveKind.GNU, out _, out diagnostics));
            ExpectNoDiagnostics(diagnostics);
        }
    }

    [TestMethod]
    public void CheckInvalidBSDFileEntry()
    {
        // Input invalid BSD Length
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
            entry.Fill((byte)' ');
            entry[0] = (byte)'#';
            entry[1] = (byte)'1';
            entry[2] = (byte)'/';
            entry[3] = (byte)'a';

            stream.Write(entry);

            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.BSD, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.AR_ERR_InvalidCharacterFoundInFileEntry);
        }

        // Input invalid BSD Length
        {
            var stream = new MemoryStream();
            stream.Write(ArArchiveFile.Magic);
            var entry = new Span<byte>(new byte[ArFile.FileEntrySizeInBytes]);
            entry.Fill((byte)' ');
            entry[0] = (byte)'#';
            entry[1] = (byte)'1';
            entry[2] = (byte)'/';
            entry[3] = (byte)'2'; // it will try to read 2 bytes for the name but won't find it

            entry[ArFile.FieldEndCharactersOffset] = (byte)'`';
            entry[ArFile.FieldEndCharactersOffset + 1] = (byte)'\n';
                
            stream.Write(entry);

            var continuePosition = stream.Position;
                
            stream.Position = 0;

            Assert.IsFalse(ArArchiveFile.TryRead(stream, ArArchiveKind.BSD, out _, out var diagnostics));
            ExpectDiagnostics(diagnostics, DiagnosticId.CMN_ERR_UnexpectedEndOfFile);

            // Check validity of BSD name

            stream.Position = continuePosition;
            stream.WriteByte((byte)'a');
            stream.WriteByte((byte)'b');

            stream.Position = 0;

            var result = ArArchiveFile.TryRead(stream, ArArchiveKind.BSD, out var arFile, out diagnostics);
            Assert.IsTrue(result, $"Error while reading file: {diagnostics}");
            Assert.AreEqual(1, arFile.Files.Count, "Invalid number of file entries found");
            Assert.AreEqual("ab", arFile.Files[0].Name, "Invalid name of file entry[0] found");
            Assert.IsNull(arFile.SymbolTable, "Invalid non-null symbol table found");
        }
    }

    [TestMethod]
    public void CheckLibraryWithELF()
    {
        var cppObj = "helloworld.o";
        var cppLib = GetFile("libhelloworld.a");

        using (var stream = new FileStream(cppLib, FileMode.Open, FileAccess.Read))
        {
            Assert.IsTrue(ArArchiveFile.IsAr(stream));

            var arFile = ArArchiveFile.Read(stream, new ArArchiveFileReaderOptions(ArArchiveKind.GNU) {ProcessObjectFiles = false});

            var elfFile = arFile.Files.FirstOrDefault(x => x.Name == cppObj);

            Assert.IsNotNull(elfFile, $"Unable to find {cppObj} file in {cppLib}");

            Assert.IsNotNull(arFile.SymbolTable, $"Unable to find symbol table in {cppLib}");

            Assert.AreEqual(1, arFile.SymbolTable.Symbols.Count, "Invalid number of symbols in Symbol table");
            Assert.AreEqual("main", arFile.SymbolTable.Symbols[0].Name, "Invalid symbol found");
            Assert.AreEqual(elfFile, arFile.SymbolTable.Symbols[0].File, "Invalid symbol to file found");

            var outStream = new MemoryStream();
            arFile.Write(outStream);
            var newArray = outStream.ToArray();
            outStream.Position = 0;

            //var cppLibCopy = $"lib{cppName}_copy.a";
            //using (var copyStream = new FileStream(cppLibCopy, FileMode.Create, FileAccess.Write))
            //{
            //    outStream.CopyTo(copyStream);
            //}

            var originalStream = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(originalStream);
            var originalArray = originalStream.ToArray();

            ByteArrayAssert.AreEqual(originalArray, newArray, $"Non binary matching for file {cppLib} ");
        }
    }

    [TestMethod]
    public async Task CheckCreateArLibrary()
    {
        var libName = "libcustom.a";

        var file = new ArArchiveFile();
        using (var stream = new FileStream(libName, FileMode.Create, FileAccess.Write))
        {
            var symbolTable = new ArSymbolTable();
            file.AddFile(symbolTable);
                
            file.AddFile(new ArBinaryFile() { Name = "file2.txt", OwnerId = 0666,  Stream = new MemoryStream(Encoding.UTF8.GetBytes("this is file")) });

            file.AddFile(new ArBinaryFile() { Name = "file3.txt", OwnerId = 0777, GroupId = 0744, Stream = new MemoryStream(Encoding.UTF8.GetBytes("this is file3")) });

            file.AddFile(new ArBinaryFile() { Name = "file4.txt", OwnerId = 0777, GroupId = 0744, Stream = new MemoryStream(Encoding.UTF8.GetBytes("this is file4")) });

            file.AddFile(new ArBinaryFile() { Name = "file5.txt", OwnerId = 0706, GroupId = 0705, FileMode = 0x123456, Stream = new MemoryStream(Encoding.UTF8.GetBytes("this is file5")) });

            file.AddFile(new ArBinaryFile() { Name = "long_file_name_large_file6.txt", Timestamp = DateTimeOffset.UtcNow.AddSeconds(-2),  Stream = new MemoryStream(Encoding.UTF8.GetBytes("this is file6 yoyo")) });

            symbolTable.Symbols.Add(new ArSymbol() { File = file.Files[1], Name = "my_symbol" });

            file.Write(stream);
            stream.Flush();
        }

        Recording.Start();

        // Check that AR is able to read back what we just serialized
        {
            var fileNameBuilder = new StringBuilder();
            foreach (var fileEntry in file.Files)
            {
                if (fileEntry.IsSystem) continue;
                fileNameBuilder.Append($"{fileEntry.Name}\n");
            }

            var fileNameList = fileNameBuilder.ToString().Trim();
            Recording.Add("filenames", fileNameList);
        }

        // Display the content of each file via AR
        {
            var contentBuilder = new StringBuilder();
            foreach (var fileEntry in file.Files)
            {
                if (!(fileEntry is ArBinaryFile arBinary)) continue;

                arBinary.Stream!.Position = 0;
                contentBuilder.Append(Encoding.UTF8.GetString(((MemoryStream) arBinary.Stream).ToArray()));
            }

            var content = contentBuilder.ToString().Trim();
            Recording.Add("filecontent", content);
        }

        await Verify();

        ArArchiveFile file2;
        using (var stream = new FileStream(libName, FileMode.Open, FileAccess.Read))
        {
            file2 = ArArchiveFile.Read(stream, ArArchiveKind.GNU);
        }

        Assert.IsNotNull(file2.SymbolTable);
        CompareArFiles(file, file2);

        var libNameBsd = "libcustom_bsd.a";
        file.Kind = ArArchiveKind.BSD;
        using (var stream = new FileStream(libNameBsd, FileMode.Create, FileAccess.Write))
        {
            file.Write(stream);
            stream.Flush();
        }

        ArArchiveFile archiveFileBsd;
        using (var stream = new FileStream(libNameBsd, FileMode.Open, FileAccess.Read))
        {
            archiveFileBsd = ArArchiveFile.Read(stream, ArArchiveKind.BSD);
        }

        CompareArFiles(file, archiveFileBsd);
    }

    private static void CompareArFiles(ArArchiveFile archiveFile, ArArchiveFile file2)
    {
        Assert.AreEqual(archiveFile.Files.Count, file2.Files.Count, "File entries count don't match");
        for (var i = 0; i < archiveFile.Files.Count; i++)
        {
            var fileEntry = archiveFile.Files[i];
            var file2Entry = file2.Files[i];
            Assert.AreEqual(fileEntry.GetType(), file2Entry.GetType(), $"File entry [{i}] for {archiveFile} type don't match ");
            Assert.AreEqual(fileEntry.Name, file2Entry.Name, $"File entry Name [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.Timestamp, file2Entry.Timestamp, $"File entry Timestamp [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.OwnerId, file2Entry.OwnerId, $"File entry Timestamp [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.GroupId, file2Entry.GroupId, $"File entry Timestamp [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.FileMode, file2Entry.FileMode, $"File entry Timestamp [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.Size, file2Entry.Size, $"File entry Size [{i}] for {archiveFile}");
            Assert.AreEqual(fileEntry.ToString(), file2Entry.ToString(), $"File entry ToString() [{i}] for {archiveFile}");

            if (fileEntry is ArSymbolTable fileSymbolTable)
            {
                var file2SymbolTable = (ArSymbolTable) (file2Entry);
                for (var j = 0; j < fileSymbolTable.Symbols.Count; j++)
                {
                    var fileSymbol = fileSymbolTable.Symbols[j];
                    var file2Symbol = file2SymbolTable.Symbols[j];

                    Assert.AreEqual(fileSymbol.ToString(), file2Symbol.ToString(), $"Invalid symbol [{j}]");
                }
            }
        }
    }
}