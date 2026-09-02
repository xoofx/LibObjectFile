// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO;
using LibObjectFile.MachO.Internal;
using VerifyTests;

namespace LibObjectFile.Tests.MachO;

[TestClass]
public class MachOSimpleTests : MachOTestBase
{
    private static List<uint> CollectOffsets(MachOFile file)
    {
        var offsets = new List<uint>();
        foreach (var command in file.LoadCommands)
        {
            command.UpdateFileOffsets(offset =>
            {
                offsets.Add(offset);
                return offset;
            });
        }
        return offsets;
    }

    /// <summary>
    /// The raw structures are copied by value straight out of the file, so a wrong size or an
    /// unexpected padding byte would silently shift every field after it.
    /// </summary>
    [TestMethod]
    public unsafe void RawStructuresMatchTheOnDiskLayout()
    {
        Assert.AreEqual(28, sizeof(RawMachHeader32));
        Assert.AreEqual(32, sizeof(RawMachHeader64));
        Assert.AreEqual(8, sizeof(RawLoadCommand));
        Assert.AreEqual(56, sizeof(RawSegmentCommand32));
        Assert.AreEqual(72, sizeof(RawSegmentCommand64));
        Assert.AreEqual(68, sizeof(RawSection32));
        Assert.AreEqual(80, sizeof(RawSection64));
        Assert.AreEqual(8, sizeof(RawFatHeader));
        Assert.AreEqual(20, sizeof(RawFatArch));
        Assert.AreEqual(32, sizeof(RawFatArch64));
    }

    [TestMethod]
    [DataRow("helloworld_x86_64", true, MachOCpuType.X86_64, MachOFileType.Execute)]
    [DataRow("helloworld_arm64", true, MachOCpuType.Arm64, MachOFileType.Execute)]
    [DataRow("libhelloworld_x86_64.dylib", true, MachOCpuType.X86_64, MachOFileType.Dylib)]
    [DataRow("helloworld_x86_64.o", true, MachOCpuType.X86_64, MachOFileType.Object)]
    [DataRow("unixthread_i386", false, MachOCpuType.X86, MachOFileType.Execute)]
    [DataRow("dyldinfo_i386", false, MachOCpuType.X86, MachOFileType.Execute)]
    public void ReadsHeader(string name, bool is64Bit, MachOCpuType cpuType, MachOFileType fileType)
    {
        var file = LoadMachO(name);

        Assert.AreEqual(is64Bit, file.Is64Bit);
        Assert.AreEqual(cpuType, file.CpuType);
        Assert.AreEqual(fileType, file.FileType);
        Assert.AreEqual(file.HeaderSize + file.SizeOfCommands, file.LoadCommandsEndOffset);
    }

    /// <summary>
    /// The padding after the load commands is what an in-place edit consumes, and a command
    /// keeps the linker's own padding rather than being re-encoded to its smallest legal size.
    /// Both are what let an untouched image round-trip byte for byte.
    /// </summary>
    [TestMethod]
    public void ReportsSpaceLeftForNewLoadCommands()
    {
        var file = LoadMachO("unixthread_i386");

        Assert.AreEqual(728u, file.LoadCommandsEndOffset);
        Assert.AreEqual(2048ul, file.ContentStartOffset);
        Assert.AreEqual(1320, file.AvailableLoadCommandSpace);
        Assert.AreEqual(1320ul, file.LoadCommandPadding!.Size);
        Assert.AreEqual(728ul, file.LoadCommandPadding.Position, "the padding follows the command table");

        var dylinker = file.LoadCommands.OfType<MachOPathCommand>().Single(c => c.Type == MachOLoadCommandType.LoadDylinker);
        Assert.AreEqual(28ul, dylinker.Size);
        Assert.AreEqual(28u, dylinker.MinimumSize);
        Assert.IsTrue(file.LoadCommands.OfType<MachODylibCommand>().All(d => d.Size >= d.MinimumSize));
    }

    /// <summary>
    /// The snapshot shows the thread state's flavour and length, but not that the eleventh word
    /// is the entry point, nor that the symbol table runs straight into the string table.
    /// </summary>
    [TestMethod]
    public void ReadsThe32BitEntryPointAndTableLayout()
    {
        var file = LoadMachO("unixthread_i386");

        var state = file.LoadCommands.OfType<MachOThreadCommand>().Single().States.Single();
        Assert.AreEqual(1u, state.Flavor, "x86_THREAD_STATE32");
        Assert.AreEqual((uint)file.FindSegment("__TEXT")!.Sections.Single().Address, state.Registers[10]);

        var symtab = file.LoadCommands.OfType<MachOSymbolTableCommand>().Single();
        Assert.AreEqual(symtab.StringOffset, symtab.SymbolOffset + symtab.SymbolCount * MachOSymbolTableCommand.GetSymbolSize(file.Is64Bit));
    }

    /// <summary>
    /// Packing is not visible in a printed image, since only the decoded version is shown.
    /// </summary>
    [TestMethod]
    public void VersionPackingRoundTrips()
    {
        Assert.AreEqual(new Version(10, 13, 0), MachOVersion.Decode(0x000A0D00));
        Assert.AreEqual(0x000A0D00u, MachOVersion.Encode(new Version(10, 13, 0)));
        Assert.AreEqual(0x0001000Au, MachOVersion.Encode(new Version(1, 0, 10)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MachOVersion.Encode(new Version(1, 256, 0)));
    }

    [TestMethod]
    public void RejectsANonMachOStream()
    {
        using var input = new MemoryStream("not a mach-o file at all"u8.ToArray());

        Assert.IsFalse(MachOFile.IsMachO(input));
        Assert.IsFalse(MachOFile.TryRead(input, out _, out var diagnostics));
        Assert.IsTrue(diagnostics.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidMagic));
    }

    [TestMethod]
    [DataRow("helloworld_x86_64")]
    [DataRow("helloworld_arm64")]
    [DataRow("chainedfixups_arm64")]
    [DataRow("libhelloworld_x86_64.dylib")]
    [DataRow("helloworld_x86_64.o")]
    [DataRow("unixthread_i386")]
    [DataRow("dyldinfo_i386")]
    public void ReadWriteIsByteExact(string name)
    {
        var original = File.ReadAllBytes(GetFile(name));

        using var input = new MemoryStream(original);
        var file = MachOFile.Read(input);

        var output = new MemoryStream();
        file.Write(output);

        ByteArrayAssert.AreEqual(original, output.ToArray(), $"Invalid binary diff for {name} after read -> write");
    }

    /// <summary>
    /// Commands this code does not model have to survive verbatim, which is what keeps the reader
    /// usable against images from a newer linker. The type used here is a made-up one, so that
    /// modelling more real commands later cannot quietly void the test.
    /// </summary>
    [TestMethod]
    public void KeepsUnmodelledLoadCommandsVerbatim()
    {
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };

        var file = LoadMachO("unixthread_i386");
        file.LoadCommands.Add(new MachOUnknownLoadCommand
        {
            Type = (MachOLoadCommandType)0x7F000001,
            Size = 16,
            Payload = payload,
        });
        file.LoadCommands.Add(new MachOTwoLevelHintsCommand
        {
            Type = MachOLoadCommandType.TwoLevelHints,
            Size = MachOTwoLevelHintsCommand.CommandSize,
            Offset = 0x2000,
            HintCount = 7,
        });

        var stream = new MemoryStream();
        file.Write(stream);
        var reread = MachOFile.Read(new MemoryStream(stream.ToArray()));

        var survivor = reread.LoadCommands.OfType<MachOUnknownLoadCommand>().Single(c => c.Type == (MachOLoadCommandType)0x7F000001);
        CollectionAssert.AreEqual(payload, survivor.Payload);

        var hints = reread.LoadCommands.OfType<MachOTwoLevelHintsCommand>().Single();
        Assert.AreEqual(0x2000u, hints.Offset);
        Assert.AreEqual(7u, hints.HintCount);
    }

    /// <summary>
    /// A layout has to be able to move everything in <c>__LINKEDIT</c>, so no command may keep a
    /// file offset the walk does not reach. Shifting them all and reading them back catches a
    /// missed field, which would otherwise still point at where the data used to be.
    /// </summary>
    [TestMethod]
    [DataRow("helloworld_x86_64")]
    [DataRow("chainedfixups_arm64")]
    [DataRow("unixthread_i386")]
    [DataRow("dyldinfo_i386")]
    public void EveryRecordedFileOffsetIsReachable(string name)
    {
        const uint delta = 0x1000;

        var originals = CollectOffsets(LoadMachO(name));
        Assert.AreNotEqual(0, originals.Count, "the fixture records no file offsets at all");

        var shiftedFile = LoadMachO(name);
        shiftedFile.UpdateFileOffsets(offset => offset + delta);
        CollectionAssert.AreEqual(originals.Select(o => o + delta).ToArray(), CollectOffsets(shiftedFile).ToArray());

        // An identity remap has to leave the image untouched.
        var identity = LoadMachO(name);
        identity.UpdateFileOffsets(offset => offset);
        var stream = new MemoryStream();
        identity.Write(stream);
        ByteArrayAssert.AreEqual(File.ReadAllBytes(GetFile(name)), stream.ToArray(), "an identity remap changed the image");
    }
}
