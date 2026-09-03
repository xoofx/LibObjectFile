// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers.Binary;
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

        // ncmds and sizeofcmds sit at the same offsets in both header layouts. Comparing the
        // decoded table against them catches a walk that read a different number of commands
        // than the file declares.
        var raw = File.ReadAllBytes(GetFile(name));
        Assert.AreEqual(BitConverter.ToUInt32(raw, 16), (uint)file.LoadCommands.Count, "ncmds");
        Assert.AreEqual(BitConverter.ToUInt32(raw, 20), file.SizeOfCommands, "sizeofcmds");
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

        // A table too large for sizeofcmds must not wrap into a small value, which would offer
        // room that does not exist and let an edit write over the content after it.
        dylinker.Size = 0x1_0000_0000;
        Assert.AreEqual(uint.MaxValue, file.SizeOfCommands, "sizeofcmds saturates rather than wrapping");
        Assert.IsTrue(file.AvailableLoadCommandSpace < 0, "a table that cannot be recorded leaves no room");
        Assert.IsTrue(file.Verify().Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit));
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
    /// Every offset pointing at relocatable data has to move when the map says so. The offsets
    /// are read back through the typed properties rather than through the same walk being tested,
    /// so a field the walk does not reach shows up here as one that failed to move.
    /// </summary>
    [TestMethod]
    [DataRow("helloworld_x86_64")]
    [DataRow("chainedfixups_arm64")]
    [DataRow("unixthread_i386")]
    [DataRow("dyldinfo_i386")]
    [DataRow("helloworld_x86_64.o")]
    public void EveryRecordedFileOffsetIsReachable(string name)
    {
        const uint delta = 0x1000;

        var before = ReadOffsets(LoadMachO(name));
        Assert.AreNotEqual(0, before.Count, "the fixture records no file offsets at all");

        var file = LoadMachO(name);
        var placementBefore = file.Segments.Select(s => (s.FileOffset, Sections: s.Sections.Select(x => x.FileOffset).ToArray())).ToArray();

        file.UpdateFileOffsets(offset => offset + delta);

        var after = ReadOffsets(file);
        CollectionAssert.AreEqual(before.Select(o => o + delta).ToArray(), after.ToArray(), "an offset the walk should reach did not move");

        // Placement is outside the contract: moving a segment or a section moves it in memory.
        var placementAfter = file.Segments.Select(s => (s.FileOffset, Sections: s.Sections.Select(x => x.FileOffset).ToArray())).ToArray();
        for (var i = 0; i < placementBefore.Length; i++)
        {
            Assert.AreEqual(placementBefore[i].FileOffset, placementAfter[i].FileOffset, "a segment was moved");
            CollectionAssert.AreEqual(placementBefore[i].Sections, placementAfter[i].Sections, "a section was moved");
        }

        // An identity remap has to leave the image untouched.
        var identity = LoadMachO(name);
        identity.UpdateFileOffsets(offset => offset);
        ByteArrayAssert.AreEqual(File.ReadAllBytes(GetFile(name)), WriteToArray(identity), "an identity remap changed the image");
    }

    /// <summary>
    /// Reads the relocatable offsets straight off the commands, independently of the walk.
    /// </summary>
    private static List<uint> ReadOffsets(MachOFile file)
    {
        var offsets = new List<uint>();
        void Add(uint value)
        {
            if (value != 0) offsets.Add(value);
        }

        foreach (var segment in file.Segments)
        {
            foreach (var section in segment.Sections) Add(section.RelocationOffset);
        }

        foreach (var command in file.LoadCommands)
        {
            switch (command)
            {
                case MachOSymbolTableCommand symtab:
                    Add(symtab.SymbolOffset); Add(symtab.StringOffset);
                    break;
                case MachODynamicSymbolTableCommand dysymtab:
                    Add(dysymtab.TableOfContentsOffset); Add(dysymtab.ModuleTableOffset);
                    Add(dysymtab.ExternalReferenceOffset); Add(dysymtab.IndirectSymbolOffset);
                    Add(dysymtab.ExternalRelocationOffset); Add(dysymtab.LocalRelocationOffset);
                    break;
                case MachODyldInfoCommand dyldInfo:
                    Add(dyldInfo.RebaseOffset); Add(dyldInfo.BindOffset); Add(dyldInfo.WeakBindOffset);
                    Add(dyldInfo.LazyBindOffset); Add(dyldInfo.ExportOffset);
                    break;
                case MachOLinkEditDataCommand data:
                    Add(data.DataOffset);
                    break;
                case MachOTwoLevelHintsCommand hints:
                    Add(hints.Offset);
                    break;
            }
        }

        return offsets;
    }


    /// <summary>
    /// Verification exists to catch the invariants nothing else does. The address check is the
    /// one the format rests on: break it and the loader maps a section somewhere other than
    /// where the code expects, which no round-trip test would notice.
    /// </summary>
    [TestMethod]
    public void VerifyAcceptsRealImagesAndCatchesABrokenOne()
    {
        foreach (var name in new[] { "helloworld_x86_64", "helloworld_arm64", "unixthread_i386", "helloworld_x86_64.o" })
        {
            var diagnostics = new DiagnosticBag();
            LoadMachO(name).Verify(diagnostics);
            Assert.IsFalse(diagnostics.HasErrors, $"{name} should verify: {string.Join("; ", diagnostics.Messages)}");
        }

        var moved = LoadMachO("unixthread_i386");
        moved.FindSegment("__TEXT")!.Sections[0].Address += 4;
        var broken = new DiagnosticBag();
        moved.Verify(broken);
        Assert.IsTrue(broken.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_SectionAddressMismatch));

        // An object file's sections carry addresses the linker has still to assign, so they do
        // not track file offsets and the check above does not apply to them. Apple's own crt1.o
        // breaks it. The committed object fixture happens to satisfy it, so the case is made
        // rather than found.
        var relocatable = LoadMachO("helloworld_x86_64.o");
        relocatable.Segments.Single().Sections[1].Address += 0x1000;
        Assert.IsFalse(relocatable.Verify().HasErrors, "an object file is not laid out at its final addresses");

        var linked = LoadMachO("helloworld_x86_64");
        Assert.AreEqual(MachOFileType.Execute, linked.FileType);
        linked.FindSegment("__TEXT")!.Sections[1].Address += 0x1000;
        Assert.IsTrue(linked.Verify().Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_SectionAddressMismatch),
            "the same edit in a linked image is what the check is for");

        var overlong = LoadMachO("unixthread_i386");
        overlong.LoadCommands[0].Size += 1;
        var misaligned = new DiagnosticBag();
        overlong.Verify(misaligned);
        Assert.IsTrue(misaligned.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidCommandAlignment));

        // A header edited on its own describes a section that is not there, and the round-trip
        // would still match because the header and the bytes are written from what each holds.
        var resized = LoadMachO("unixthread_i386");
        resized.FindSegment("__TEXT")!.Sections[0].Size += 8;
        var mismatched = new DiagnosticBag();
        resized.Verify(mismatched);
        Assert.IsTrue(mismatched.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_SectionContentMismatch));
    }

    /// <summary>
    /// The padding after the load command table is the only elastic thing in the file, so it has
    /// to absorb exactly what the table gains and nothing else may shift. This is what an
    /// install_name_tool-style edit depends on.
    /// </summary>
    [TestMethod]
    public void GrowingTheCommandTableConsumesOnlyThePadding()
    {
        var file = LoadMachO("unixthread_i386");

        var paddingBefore = file.LoadCommandPadding!.Size;
        var tableEndBefore = file.LoadCommandsEndOffset;
        var contentStart = file.ContentStartOffset;
        var positionsBefore = file.Content.Select(c => (c.GetType().Name, c.Position)).ToArray();

        var added = file.AddRPath("@executable_path/../Frameworks");
        var image = WriteToArray(file);

        Assert.AreEqual(tableEndBefore + added.Size, file.LoadCommandsEndOffset);
        Assert.AreEqual(paddingBefore - added.Size, file.LoadCommandPadding!.Size, "the padding did not absorb the new command");
        Assert.AreEqual(contentStart, file.ContentStartOffset, "content after the padding moved");

        // Only the padding may have moved; everything after it stays exactly where it was.
        var positionsAfter = file.Content.Select(c => (c.GetType().Name, c.Position)).ToArray();
        for (var i = 0; i < positionsBefore.Length; i++)
        {
            if (positionsBefore[i].Name == nameof(MachOLoadCommandPadding)) continue;
            Assert.AreEqual(positionsBefore[i], positionsAfter[i], $"content {i} moved");
        }

        Assert.AreEqual(new FileInfo(GetFile("unixthread_i386")).Length, image.Length, "the file changed size");
    }

    /// <summary>
    /// Names and values cross-checked against llvm-nm. The object file is included because it
    /// keeps its tables past the end of its only segment, so they belong to no segment and are
    /// reached by a different path than a linked image's __LINKEDIT.
    /// </summary>
    [TestMethod]
    public void ReadsTheSymbolTable()
    {
        var symbols = LoadMachO("helloworld_x86_64").ReadSymbolTable();

        CollectionAssert.AreEqual(
            new[] { "__mh_execute_header", "_helloworld_data", "_helloworld_twice", "_main", "_printf", "dyld_stub_binder" },
            symbols.Select(s => s.Name).ToArray());

        var main = symbols.Single(s => s.Name == "_main");
        Assert.AreEqual(MachOSymbolKind.Section, main.Kind);
        Assert.IsTrue(main.IsExternal);
        Assert.IsFalse(main.IsDebug);
        Assert.AreEqual(1, main.SectionIndex, "__text is the first section");
        Assert.AreEqual(0x100000f50ul, main.Value);

        var printf = symbols.Single(s => s.Name == "_printf");
        Assert.AreEqual(MachOSymbolKind.Undefined, printf.Kind);
        Assert.IsTrue(printf.IsExternal);
        Assert.AreEqual(0, printf.SectionIndex);
        Assert.AreEqual(0ul, printf.Value);
        Assert.AreEqual(1, printf.LibraryOrdinal, "resolved from the first dylib, libSystem");

        var fromObject = LoadMachO("helloworld_x86_64.o").ReadSymbolTable();
        CollectionAssert.AreEqual(
            new[] { "_helloworld_data", "_helloworld_twice", "_main", "_printf" },
            fromObject.Select(s => s.Name).ToArray());
        Assert.AreEqual(MachOSymbolKind.Undefined, fromObject.Single(s => s.Name == "_printf").Kind);

        // The stub sections index into the indirect table through reserved1, and not every entry
        // there is an index: the sentinels mark slots the loader has nothing to bind.
        var file = LoadMachO("helloworld_x86_64");
        var indirect = file.ReadIndirectSymbolTable();
        Assert.AreEqual(4, indirect.Count);
        CollectionAssert.Contains(indirect.ToArray(), MachOFile.IndirectSymbolAbsolute);

        var stubs = file.Segments.SelectMany(s => s.Sections).Single(s => s.Name == "__stubs");
        Assert.AreEqual(MachOSectionType.SymbolStubs, stubs.SectionType);
        Assert.AreEqual("_printf", symbols[(int)indirect[(int)stubs.Reserved1]].Name);

        // A 32-bit nlist packs into 12 bytes rather than 16, so it is a separate decode.
        var i386 = LoadMachO("dyldinfo_i386").ReadSymbolTable();
        CollectionAssert.AreEqual(new[] { "_main", "_dyldinfo_data" }, i386.Select(s => s.Name).ToArray());
        Assert.AreEqual(0x800ul, i386[0].Value);
        Assert.AreEqual(1, i386[0].SectionIndex);
        Assert.AreEqual(0x2000ul, i386[1].Value);
        Assert.AreEqual(2, i386[1].SectionIndex);

        // arm64 shares the 64-bit decode with x86_64, so this covers the fixture rather than
        // another path through the reader.
        var arm64 = LoadMachO("helloworld_arm64").ReadSymbolTable();
        Assert.AreEqual(0x100003F24ul, arm64.Single(s => s.Name == "_main").Value);
        Assert.AreEqual(MachOSymbolKind.Undefined, arm64.Single(s => s.Name == "_printf").Kind);
    }

    /// <summary>
    /// Values cross-checked against llvm-objdump. The two forms of entry pack their fields into
    /// the same eight bytes in different orders, so encoding is checked to round-trip as well:
    /// a field unpacked from the wrong bit still reads back consistently on its own.
    /// </summary>
    [TestMethod]
    public void ReadsRelocations()
    {
        var file = LoadMachO("helloworld_x86_64.o");
        var text = file.Segments.SelectMany(s => s.Sections).Single(s => s.Name == "__text");
        var symbols = file.ReadSymbolTable();

        var relocations = file.ReadRelocations(text);
        Assert.AreEqual(4, relocations.Count);

        var branch = relocations[0];
        Assert.AreEqual(0x36, branch.Address);
        Assert.AreEqual((byte)MachOX86_64RelocationType.Branch, branch.RawType);
        Assert.IsTrue(branch.IsPcRelative);
        Assert.IsTrue(branch.IsExternal);
        Assert.AreEqual(4, branch.LengthInBytes);
        Assert.IsFalse(branch.IsScattered);
        Assert.AreEqual("_printf", symbols[(int)branch.SymbolOrSectionNumber].Name);

        // A local entry numbers a section instead of a symbol.
        var signed = relocations[1];
        Assert.AreEqual((byte)MachOX86_64RelocationType.Signed, signed.RawType);
        Assert.IsFalse(signed.IsExternal);
        Assert.AreEqual(3u, signed.SymbolOrSectionNumber);

        Assert.AreEqual("_helloworld_data", symbols[(int)relocations[3].SymbolOrSectionNumber].Name);

        foreach (var relocation in relocations)
        {
            var (word0, word1) = relocation.Encode();
            var again = MachORelocation.Decode(word0, word1);
            Assert.AreEqual(relocation.ToString(), again.ToString());
            Assert.AreEqual(relocation.SymbolOrSectionNumber, again.SymbolOrSectionNumber);
        }

        // The scattered form has no external flag and carries a target address instead.
        var scattered = MachORelocation.Decode(MachORelocation.ScatteredMask | (2u << 28) | (1u << 24) | 0x123, 0x4567);
        Assert.IsTrue(scattered.IsScattered);
        Assert.AreEqual(0x123, scattered.Address);
        Assert.AreEqual(1, scattered.RawType);
        Assert.AreEqual(4, scattered.LengthInBytes);
        Assert.AreEqual(0x4567, scattered.Value);
        Assert.IsFalse(scattered.IsExternal);
        Assert.AreEqual((MachORelocation.ScatteredMask | (2u << 28) | (1u << 24) | 0x123, 0x4567u), scattered.Encode());
    }

    /// <summary>
    /// A universal binary stores its header big-endian whatever the architectures inside are,
    /// which is the one place the format departs from the image's own byte order.
    /// </summary>
    [TestMethod]
    public void ReadsAndWritesAUniversalBinary()
    {
        var original = File.ReadAllBytes(GetFile("helloworld_fat"));

        using var input = new MemoryStream(original);
        Assert.IsTrue(MachOFatFile.IsFat(input));
        Assert.IsFalse(MachOFile.IsMachO(input), "a universal binary is not itself a Mach-O image");

        var fat = MachOFatFile.Read(input);
        Assert.IsFalse(fat.Is64BitOffsets);
        CollectionAssert.AreEqual(new[] { MachOCpuType.X86_64, MachOCpuType.Arm64 }, fat.Slices.Select(s => s.CpuType).ToArray());

        foreach (var slice in fat.Slices)
        {
            Assert.IsNotNull(slice.File);
            Assert.AreEqual(slice.CpuType, slice.File.CpuType, "the slice table has to agree with the image it points at");
            Assert.AreEqual(0ul, slice.FileOffset % slice.Alignment, "a slice is mapped directly, so it has to be page aligned");
        }

        Assert.IsFalse(fat.Verify().HasErrors, "a real universal binary has to verify");

        var output = new MemoryStream();
        fat.Write(output);
        ByteArrayAssert.AreEqual(original, output.ToArray(), "Invalid binary diff for helloworld_fat after read -> write");

        // Two slices claiming the same architecture leave the loader picking the first and the
        // other unreachable, so the write reports it rather than producing a file lipo rejects.
        var duplicated = MachOFatFile.Read(new MemoryStream(original));
        duplicated.Slices[1].CpuType = duplicated.Slices[0].CpuType;
        duplicated.Slices[1].CpuSubType = duplicated.Slices[0].CpuSubType;
        Assert.IsFalse(duplicated.TryWrite(new MemoryStream(), out var duplicateDiagnostics));
        Assert.IsTrue(duplicateDiagnostics.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_DuplicateFatSlice), string.Join("; ", duplicateDiagnostics.Messages));

        // A slice that no longer fits the 32-bit slice table has to be reported, since casting it
        // out would record a different slice than the one written.
        var tooFar = MachOFatFile.Read(new MemoryStream(original));
        tooFar.Slices[1].FileOffset = 0x1_0000_0000;
        tooFar.Slices[1].Size = 0x10;
        Assert.IsTrue(tooFar.Verify().Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit));

        // The table is what the loader uses to select a slice, so it has to describe the image
        // that will actually be found there.
        var mismatched = MachOFatFile.Read(new MemoryStream(original));
        mismatched.Slices[0].CpuType = MachOCpuType.X86;
        Assert.IsFalse(mismatched.TryWrite(new MemoryStream(), out var mismatchDiagnostics));
        Assert.IsTrue(mismatchDiagnostics.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_FatSliceArchitectureMismatch), string.Join("; ", mismatchDiagnostics.Messages));
    }

    /// <summary>
    /// Snapshots the decoded form of each fixture. This covers every field of every command at
    /// once, so decoding a new command extends the snapshot rather than needing another test,
    /// and a change to how anything is read shows up as a diff rather than passing unnoticed.
    /// </summary>
    [TestMethod]
    [DataRow("helloworld_x86_64")]
    [DataRow("helloworld_arm64")]
    [DataRow("chainedfixups_arm64")]
    [DataRow("libhelloworld_x86_64.dylib")]
    [DataRow("helloworld_x86_64.o")]
    [DataRow("unixthread_i386")]
    [DataRow("dyldinfo_i386")]
    public async Task Prints(string name)
    {
        await VerifyMachO(LoadMachO(name), name);
    }


    /// <summary>
    /// A command is bounded by its own cmdsize and the table by the header's sizeofcmds. Without
    /// that, a command declaring a size too small for its own fields still reads them, taking
    /// bytes that belong to whatever follows and decoding something the file does not say.
    /// </summary>
    [TestMethod]
    public void RejectsMalformedLoadCommands()
    {
        static byte[] Patch(Action<byte[]> corrupt)
        {
            var bytes = File.ReadAllBytes(GetFile("unixthread_i386"));
            corrupt(bytes);
            return bytes;
        }

        static uint Read(byte[] b, int offset) => BitConverter.ToUInt32(b, offset);
        static void Write(byte[] b, int offset, uint value) => BitConverter.GetBytes(value).CopyTo(b, offset);

        // Finds where a load command starts, so the patches below do not depend on offsets that
        // would silently point at the wrong field if a fixture were regenerated.
        static int FindCommand(byte[] b, uint type, int skip)
        {
            var is64 = Read(b, 0) == 0xfeedfacf;
            var offset = is64 ? 32 : 28;
            for (var i = 0; i < Read(b, 16); i++)
            {
                if (Read(b, offset) == type && skip-- == 0) return offset;
                offset += (int)Read(b, offset + 4);
            }

            throw new InvalidOperationException($"No load command of type 0x{type:X} in the fixture");
        }

        // The first command is LC_SEGMENT at offset 28. Shrinking its cmdsize below its fixed
        // part would have it read fields out of the command after it.
        var tooSmall = Patch(b => Write(b, 32, 8));
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(tooSmall), out _, out var d1));
        Assert.IsTrue(d1.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidLoadCommandSize), string.Join("; ", d1.Messages));

        // A table that does not end where the header says means the walk and the header disagree
        // about which bytes are commands.
        var shortTable = Patch(b => Write(b, 20, Read(b, 20) - 8));
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(shortTable), out _, out var d2));
        Assert.IsTrue(d2.Messages.Any(m => m.Id is DiagnosticId.MACHO_ERR_LoadCommandTableSizeMismatch
            or DiagnosticId.MACHO_ERR_TruncatedLoadCommand), string.Join("; ", d2.Messages));

        // A section count that does not fit the command would read section headers out of the
        // commands after it.
        var tooManySections = Patch(b => Write(b, 28 + 56 + 48, 40));
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(tooManySections), out _, out var d3));
        Assert.IsTrue(d3.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidLoadCommandSize), string.Join("; ", d3.Messages));

        // A count chosen so that multiplying it out wraps: 0x40000001 sections at 68 bytes each
        // comes back to 124, the size of the command being checked, so a check that multiplies
        // would let it through and the section headers would be read out of the commands after it.
        var wrappingSections = Patch(b => Write(b, FindCommand(b, 0x1, skip: 1) + 48, 0x40000001));
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(wrappingSections), out _, out var d5));
        Assert.IsTrue(d5.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidLoadCommandSize), string.Join("; ", d5.Messages));

        // The same for a tool count: 0x20000000 tools at 8 bytes each wraps to nothing at all.
        var arm64 = File.ReadAllBytes(GetFile("helloworld_arm64"));
        Write(arm64, FindCommand(arm64, 0x32, skip: 0) + 20, 0x20000000);
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(arm64), out _, out var d6));
        Assert.IsTrue(d6.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidLoadCommandSize), string.Join("; ", d6.Messages));

        // A count large enough that multiplying it by an entry size wraps a 32-bit product would
        // otherwise pass a length check as a small number and then be read past.
        var overflowing = LoadMachO("helloworld_x86_64");
        overflowing.LoadCommands.OfType<MachOSymbolTableCommand>().Single().SymbolCount = 0x10000001;
        Assert.ThrowsExactly<ObjectFileException>(() => overflowing.ReadSymbolTable());

        // A universal binary header claiming more slices than the file holds must not be trusted
        // to size anything before that is checked.
        var fat = File.ReadAllBytes(GetFile("helloworld_fat"));
        BitConverter.GetBytes(0x10000000u).Reverse().ToArray().CopyTo(fat, 4);
        Assert.IsFalse(MachOFatFile.TryRead(new MemoryStream(fat), out _, out var d4));
        Assert.IsTrue(d4.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidFatHeader), string.Join("; ", d4.Messages));

        // lipo refuses an alignment past 2^15, and a shift count is masked to the width of what
        // it shifts, so an unchecked exponent would alias to a different alignment rather than
        // being rejected. The align field of the first slice sits at the end of its 20-byte
        // entry, and the slice table is big-endian.
        var badAlign = File.ReadAllBytes(GetFile("helloworld_fat"));
        BitConverter.GetBytes(MachOFatSlice.MaxAlignLog2 + 1).Reverse().ToArray().CopyTo(badAlign, MachOFatFile.HeaderSize + 16);
        Assert.IsFalse(MachOFatFile.TryRead(new MemoryStream(badAlign), out _, out var d7));
        Assert.IsTrue(d7.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidFatSliceAlignment), string.Join("; ", d7.Messages));
    }

    /// <summary>
    /// A Try method owes the caller diagnostics rather than an exception, whatever it is handed.
    /// The magic was read without a bound, so anything shorter than four bytes escaped as an
    /// <see cref="EndOfStreamException"/>, as did a universal binary slice with no room for a
    /// header.
    /// </summary>
    [TestMethod]
    public void ReportsRatherThanThrowsOnUnreadableInput()
    {
        using var input = new MemoryStream("not a mach-o file at all"u8.ToArray());

        Assert.IsFalse(MachOFile.IsMachO(input));
        Assert.IsFalse(MachOFile.TryRead(input, out _, out var diagnostics));
        Assert.IsTrue(diagnostics.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidMagic));

        // A universal static library is a fat file of ar archives rather than images, which is
        // a real thing to be handed and worth naming instead of reporting a stray magic.
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream("!<arch>\n"u8.ToArray()), out _, out var archive));
        Assert.IsTrue(archive.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_UnexpectedArchive), string.Join("; ", archive.Messages));

        foreach (var length in new[] { 0, 1, 3 })
        {
            Assert.IsFalse(MachOFile.TryRead(new MemoryStream(new byte[length]), out _, out var tooShort), $"a {length}-byte stream cannot be an image");
            Assert.IsTrue(tooShort.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidMagic), string.Join("; ", tooShort.Messages));
        }

        // Every truncation of a real image, which is what walks each bounded read up to its
        // limit. Anything that throws here fails the test by escaping.
        var image = File.ReadAllBytes(GetFile("unixthread_i386"));
        for (var length = 0; length < image.Length; length += 7)
        {
            MachOFile.TryRead(new MemoryStream(image.AsSpan(0, length).ToArray()), out _, out _);
        }

        // The size of the first slice sits 12 bytes into its entry, and the slice table is
        // big-endian. A slice with no room for a header must be rejected, not descended into.
        var fat = File.ReadAllBytes(GetFile("helloworld_fat"));
        BitConverter.GetBytes(0u).Reverse().ToArray().CopyTo(fat, MachOFatFile.HeaderSize + 12);
        Assert.IsFalse(MachOFatFile.TryRead(new MemoryStream(fat), out _, out var emptySlice));
        Assert.IsTrue(emptySlice.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidFatArchRange), string.Join("; ", emptySlice.Messages));

        // FAT64 offsets and sizes are unsigned 64-bit fields. An end that wraps must be rejected
        // before either value is narrowed for SubStream.
        var wrappingFatRange = new byte[MachOFatFile.HeaderSize + MachOFatFile.GetSliceEntrySize(true)];
        BinaryPrimitives.WriteUInt32BigEndian(wrappingFatRange, MachOMagic.FatMagic64);
        BinaryPrimitives.WriteUInt32BigEndian(wrappingFatRange.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt64BigEndian(wrappingFatRange.AsSpan(MachOFatFile.HeaderSize + 8), ulong.MaxValue - 15);
        BinaryPrimitives.WriteUInt64BigEndian(wrappingFatRange.AsSpan(MachOFatFile.HeaderSize + 16), MachOFile.MinHeaderSize);
        Assert.IsFalse(MachOFatFile.TryRead(new MemoryStream(wrappingFatRange), out _, out var wrappingFatSlice));
        Assert.IsTrue(wrappingFatSlice.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidFatArchRange), string.Join("; ", wrappingFatSlice.Messages));

        // The same overflow is possible in a 64-bit section size. Build the smallest image with
        // one LC_SEGMENT_64 and one section whose declared end wraps back into the file.
        const int machHeaderSize = 32;
        const int segmentCommandSize = 72;
        const int sectionSize = 80;
        var wrappingSectionRange = new byte[machHeaderSize + segmentCommandSize + sectionSize];
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange, MachOMagic.Magic64);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(4), (uint)MachOCpuType.X86_64);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(12), (uint)MachOFileType.Object);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(16), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(20), segmentCommandSize + sectionSize);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(machHeaderSize), (uint)MachOLoadCommandType.Segment64);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(machHeaderSize + 4), segmentCommandSize + sectionSize);
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(machHeaderSize + 64), 1);
        var sectionOffset = machHeaderSize + segmentCommandSize;
        BinaryPrimitives.WriteUInt64LittleEndian(wrappingSectionRange.AsSpan(sectionOffset + 40), unchecked(ulong.MaxValue - (ulong)wrappingSectionRange.Length + 11));
        BinaryPrimitives.WriteUInt32LittleEndian(wrappingSectionRange.AsSpan(sectionOffset + 48), (uint)wrappingSectionRange.Length);
        Assert.IsFalse(MachOFile.TryRead(new MemoryStream(wrappingSectionRange), out _, out var wrappingSection));
        Assert.IsTrue(wrappingSection.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_InvalidContentFileRange), string.Join("; ", wrappingSection.Messages));
    }
}
