// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibObjectFile.Elf;

namespace LibObjectFile.Tests.Elf;

[TestClass]
public class ElfDynamicEditingTests : ElfTestBase
{
    [TestMethod]
    public void AddNeededLibraryInjectsDependency()
    {
        // libstdc++.so is a real shared object with existing DT_NEEDED entries and a PT_NOTE to repurpose.
        var elf = LoadElf("libstdc++.so");

        var originalNeeded = elf.Sections.OfType<ElfDynamicLinkingTable>().Single().GetNeededLibraries().ToList();
        int notesBefore = elf.Segments.Count(s => s.Type == ElfSegmentTypeCore.Note);
        int loadsBefore = elf.Segments.Count(s => s.Type == ElfSegmentTypeCore.Load);
        ulong dynamicVaddrBefore = elf.Segments.Single(s => s.Type == ElfSegmentTypeCore.Dynamic).VirtualAddress;

        elf.AddNeededLibrary("libinject.so");

        // A PT_NOTE was repurposed into a PT_LOAD, and PT_DYNAMIC now points into the appended region.
        Assert.AreEqual(notesBefore - 1, elf.Segments.Count(s => s.Type == ElfSegmentTypeCore.Note));
        Assert.AreEqual(loadsBefore + 1, elf.Segments.Count(s => s.Type == ElfSegmentTypeCore.Load));
        Assert.IsTrue(elf.Segments.Single(s => s.Type == ElfSegmentTypeCore.Dynamic).VirtualAddress > dynamicVaddrBefore,
            "PT_DYNAMIC should have moved into the appended high-address region");

        // Write, re-read, then resolve DT_NEEDED exactly as the loader would: follow PT_DYNAMIC to the
        // dynamic array, then DT_STRTAB to turn each DT_NEEDED offset into a name.
        var stream = new MemoryStream();
        elf.Write(stream);
        var fileBytes = stream.ToArray();
        var roundTrip = ElfFile.Read(new MemoryStream(fileBytes));

        var needed = ResolveNeededViaProgramHeaders(roundTrip, fileBytes);

        CollectionAssert.IsSubsetOf(originalNeeded, needed, "existing dependencies must be preserved");
        CollectionAssert.Contains(needed, "libinject.so", "the injected dependency must be resolvable via DT_STRTAB");
        Assert.AreEqual(originalNeeded.Count + 1, needed.Count, "exactly one DT_NEEDED should be added");

        // The section view must agree with the segment/loader view: exactly one .dynamic section, and reading
        // DT_NEEDED through the section (its linked .dynstr) resolves the injected library too. This is what a
        // section-based re-reader (e.g. a patch applicator checking for idempotency) sees.
        var dynamicSections = roundTrip.Sections.OfType<ElfDynamicLinkingTable>().ToList();
        Assert.AreEqual(1, dynamicSections.Count, "there must be exactly one .dynamic section after injection");
        var neededViaSection = dynamicSections[0].GetNeededLibraries().ToList();
        CollectionAssert.AreEqual(needed, neededViaSection, "section view must match the program-header view");
    }

    [TestMethod]
    public void AddNeededLibraryThrowsForNonDynamicFile()
    {
        // Relocatable object files have no .dynamic section, so injection is not applicable.
        var elf = LoadElf("small_debug.o");
        Assert.ThrowsExactly<InvalidOperationException>(() => elf.AddNeededLibrary("libinject.so"));
    }

    // Emulates the dynamic loader: PT_DYNAMIC -> dynamic array -> DT_STRTAB -> DT_NEEDED names.
    private static List<string> ResolveNeededViaProgramHeaders(ElfFile elf, byte[] fileBytes)
    {
        bool little = elf.Encoding == ElfEncoding.Lsb;
        bool is32 = elf.FileClass == ElfFileClass.Is32;
        int entrySize = is32 ? 8 : 16;

        var dynamicSegment = elf.Segments.Single(s => s.Type == ElfSegmentTypeCore.Dynamic);
        int count = (int)(dynamicSegment.Size / (ulong)entrySize);

        ulong strTabVaddr = 0;
        var neededOffsets = new List<ulong>();
        for (int i = 0; i < count; i++)
        {
            int offset = (int)dynamicSegment.Position + i * entrySize;
            long tag = is32 ? ReadU32(fileBytes, offset, little) : (long)ReadU64(fileBytes, offset, little);
            ulong value = is32 ? ReadU32(fileBytes, offset + 4, little) : ReadU64(fileBytes, offset + 8, little);

            if (tag == (long)ElfDynamicTag.StrTab) strTabVaddr = value;
            else if (tag == (long)ElfDynamicTag.Needed) neededOffsets.Add(value);
        }

        ulong strTabFileOffset = VirtualToFileOffset(elf, strTabVaddr);
        return neededOffsets.Select(o => ReadCString(fileBytes, (int)(strTabFileOffset + o))).ToList();
    }

    private static ulong VirtualToFileOffset(ElfFile elf, ulong virtualAddress)
    {
        foreach (var segment in elf.Segments)
        {
            if (segment.Type != ElfSegmentTypeCore.Load) continue;
            if (virtualAddress >= segment.VirtualAddress && virtualAddress < segment.VirtualAddress + segment.Size)
            {
                return segment.Position + (virtualAddress - segment.VirtualAddress);
            }
        }

        throw new InvalidOperationException($"Virtual address 0x{virtualAddress:x} is not mapped by any PT_LOAD segment");
    }

    private static uint ReadU32(byte[] bytes, int offset, bool little)
        => little
            ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4))
            : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));

    private static ulong ReadU64(byte[] bytes, int offset, bool little)
        => little
            ? BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, 8))
            : BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(offset, 8));

    private static string ReadCString(byte[] bytes, int offset)
    {
        int end = offset;
        while (end < bytes.Length && bytes[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(bytes, offset, end - offset);
    }
}
