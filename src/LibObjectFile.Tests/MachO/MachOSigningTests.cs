// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LibObjectFile.MachO;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.CodeSign;

namespace LibObjectFile.Tests.MachO;

using static MachOCodeSignatureConstants;

[TestClass]
public class MachOSigningTests : MachOTestBase
{
    private static byte[] SignAndWrite(string fixture, string identifier)
    {
        var file = LoadMachO(fixture);
        file.AdHocSign(identifier);
        var stream = new MemoryStream();
        file.Write(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Recomputes every page digest from the written image and compares it with what the code
    /// directory claims. This is the property the kernel checks, so getting it wrong produces a
    /// file that looks signed and is refused at execution.
    /// </summary>
    [TestMethod]
    [DataRow("unixthread_i386")]
    [DataRow("helloworld_x86_64")]
    [DataRow("helloworld_arm64")]
    [DataRow("libhelloworld_x86_64.dylib")]
    public void EveryPageDigestMatchesTheImage(string fixture)
    {
        var image = SignAndWrite(fixture, fixture);
        var file = MachOFile.Read(new MemoryStream(image));

        var command = file.CodeSignature;
        Assert.IsNotNull(command);

        var codeDirectory = FindCodeDirectory(image, command.DataOffset);
        var hashOffset = BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(16));
        var codeSlots = BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(28));
        var codeLimit = BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(32));

        Assert.AreEqual(command.DataOffset, codeLimit, "the signature must cover exactly the bytes before it");
        Assert.AreEqual((codeLimit + PageSize - 1) / PageSize, codeSlots);

        for (var slot = 0; slot < codeSlots; slot++)
        {
            var start = slot * PageSize;
            var length = Math.Min(PageSize, (int)codeLimit - start);
            var expected = SHA256.HashData(image.AsSpan(start, length));
            var stored = codeDirectory.Slice((int)hashOffset + slot * Sha256Size, Sha256Size);
            CollectionAssert.AreEqual(expected, stored.ToArray(), $"page {slot} digest does not match");
        }
    }

    [TestMethod]
    public void SignatureUsesTheAdHocCodeDirectoryFormat()
    {
        var image = SignAndWrite("unixthread_i386", "swkotor");
        var file = MachOFile.Read(new MemoryStream(image));
        var command = file.CodeSignature!;

        // Three blobs, which is what codesign produces for an ad-hoc signature: a code directory,
        // an empty requirement set and an empty CMS wrapper. The wrapper is present and empty
        // rather than omitted, matching a shipping ad-hoc signed dylib byte for byte in shape.
        var superBlob = image.AsSpan((int)command.DataOffset);
        Assert.AreEqual(EmbeddedSignatureMagic, BinaryPrimitives.ReadUInt32BigEndian(superBlob));
        Assert.AreEqual(3u, BinaryPrimitives.ReadUInt32BigEndian(superBlob.Slice(8)), "code directory, requirements and CMS slots");

        var emptyRequirements = FindBlob(image, command.DataOffset, RequirementsSlot);
        Assert.AreEqual(12, emptyRequirements.Length, "an empty requirement set");
        var cms = FindBlob(image, command.DataOffset, SignatureSlot);
        Assert.AreEqual(BlobWrapperMagic, BinaryPrimitives.ReadUInt32BigEndian(cms));
        Assert.AreEqual(8, cms.Length, "an empty CMS wrapper, as codesign writes for ad-hoc");

        var codeDirectory = FindCodeDirectory(image, command.DataOffset);
        Assert.AreEqual(CodeDirectoryMagic, BinaryPrimitives.ReadUInt32BigEndian(codeDirectory));
        Assert.AreEqual(CodeDirectoryVersionExecSeg, BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(8)));
        Assert.AreEqual(AdHocFlag, BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(12)));
        Assert.AreEqual((uint)CodeDirectoryHeaderSize, BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(20)), "identifier follows the fixed header");
        Assert.AreEqual(2u, BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(24)), "Info and requirements special slots");
        Assert.AreEqual(Sha256Size, codeDirectory[36]);
        Assert.AreEqual(HashTypeSha256, codeDirectory[37]);
        Assert.AreEqual(PageSizeLog2, codeDirectory[39]);

        var identifier = codeDirectory.Slice(CodeDirectoryHeaderSize);
        Assert.AreEqual("swkotor", Encoding.UTF8.GetString(identifier.Slice(0, identifier.IndexOf((byte)0))));

        // The executable range comes from __TEXT, and an executable is flagged as a main binary.
        var text = file.FindSegment("__TEXT")!;
        Assert.AreEqual(text.FileOffset, BinaryPrimitives.ReadUInt64BigEndian(codeDirectory.Slice(64)));
        Assert.AreEqual(text.FileSize, BinaryPrimitives.ReadUInt64BigEndian(codeDirectory.Slice(72)));
        Assert.AreEqual(ExecSegMainBinary, BinaryPrimitives.ReadUInt64BigEndian(codeDirectory.Slice(80)));

                                var hashOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(codeDirectory.Slice(16));

        var info = codeDirectory.Slice(hashOffset - Sha256Size, Sha256Size);
        Assert.IsTrue(info.ToArray().All(b => b == 0), "the Info.plist slot should be zero");

        var requirements = FindBlob(image, command.DataOffset, RequirementsSlot);
        Assert.AreEqual(RequirementsMagic, BinaryPrimitives.ReadUInt32BigEndian(requirements));
        Assert.AreEqual(0u, BinaryPrimitives.ReadUInt32BigEndian(requirements.Slice(8)), "an ad-hoc signature states no requirements");

        var stored = codeDirectory.Slice(hashOffset - 2 * Sha256Size, Sha256Size);
        CollectionAssert.AreEqual(SHA256.HashData(requirements.ToArray()), stored.ToArray());
    }

    /// <summary>
    /// Re-signing has to replace the previous signature rather than stack a new one behind it,
    /// otherwise repeated edits would grow the file without bound.
    /// </summary>
    [TestMethod]
    public void ReSigningReplacesTheExistingSignature()
    {
        var file = LoadMachO("helloworld_arm64");
        Assert.IsNotNull(file.CodeSignature);
        var commands = file.LoadCommands.Count;

        file.AdHocSign("helloworld");
        var first = new MemoryStream();
        file.Write(first);

        var again = MachOFile.Read(new MemoryStream(first.ToArray()));
        again.AdHocSign("helloworld");
        var second = new MemoryStream();
        again.Write(second);

        Assert.AreEqual(commands, again.LoadCommands.Count, "re-signing added a load command");
        Assert.AreEqual(first.Length, second.Length, "re-signing grew the file");
        ByteArrayAssert.AreEqual(first.ToArray(), second.ToArray(), "re-signing is not idempotent");
    }

    [TestMethod]
    public void SignedImagesRoundTrip()
    {
        var image = SignAndWrite("unixthread_i386", "swkotor");

        var reread = MachOFile.Read(new MemoryStream(image));
        var again = new MemoryStream();
        reread.Write(again);

        ByteArrayAssert.AreEqual(image, again.ToArray(), "a signed image does not round-trip");
    }

    /// <summary>
    /// Signing appends to __LINKEDIT, so it must not disturb anything mapped before it.
    /// </summary>
    [TestMethod]
    public void SigningLeavesEverySectionWhereItWas()
    {
        var before = LoadMachO("unixthread_i386");
        var originalOffsets = before.Segments.SelectMany(s => s.Sections).Select(s => (s.Name, s.FileOffset)).ToArray();

        var image = SignAndWrite("unixthread_i386", "swkotor");
        var after = MachOFile.Read(new MemoryStream(image));

        CollectionAssert.AreEqual(originalOffsets, after.Segments.SelectMany(s => s.Sections).Select(s => (s.Name, s.FileOffset)).ToArray());

        var linkEdit = after.FindSegment("__LINKEDIT")!;
        Assert.AreEqual(before.FindSegment("__LINKEDIT")!.FileOffset, linkEdit.FileOffset, "__LINKEDIT moved");
        Assert.IsTrue(linkEdit.FileSize > before.FindSegment("__LINKEDIT")!.FileSize, "__LINKEDIT did not grow to cover the signature");
        Assert.AreEqual(linkEdit.FileEndOffset, after.CodeSignature!.DataOffset + after.CodeSignature.DataSize);

        // The image was unsigned, so signing had to add the command as well as the signature.
        Assert.IsNull(before.CodeSignature);
        Assert.AreEqual(before.LoadCommands.Count + 1, after.LoadCommands.Count);
        Assert.AreEqual(0u, after.CodeSignature.DataOffset % SignatureAlignment, "the signature must be 16-byte aligned");
    }

    /// <summary>
    /// Editing after signing leaves the signature covering bytes that are no longer there. The
    /// result would look signed and be refused at execution, so writing it has to fail rather
    /// than hand back something broken.
    /// </summary>
    [TestMethod]
    public void EditingAfterSigningIsRejectedUntilSignedAgain()
    {
        var file = LoadMachO("unixthread_i386");
        file.AdHocSign("swkotor");
        Assert.IsFalse(file.IsCodeSignatureStale);

        file.AddRPath("@executable_path/../Frameworks");
        Assert.IsTrue(file.IsCodeSignatureStale);

        var exception = Assert.ThrowsExactly<ObjectFileException>(() => WriteToArray(file));
        Assert.IsTrue(exception.Diagnostics.Messages.Any(m => m.Id == DiagnosticId.MACHO_ERR_StaleCodeSignature));

        // Signing again makes it whole, and the digests then cover the edit.
        file.AdHocSign("swkotor");
        Assert.IsFalse(file.IsCodeSignatureStale);
        var image = WriteToArray(file);
        Assert.AreEqual("@executable_path/../Frameworks", MachOFile.Read(new MemoryStream(image)).RunPaths.Single().Path);
    }

    [TestMethod]
    public void RejectsWhatCannotBeSigned()
    {
        // An object file has no __LINKEDIT, so there is nowhere for a signature to live.
        Assert.ThrowsExactly<InvalidOperationException>(() => LoadMachO("helloworld_x86_64.o").AdHocSign("helloworld"));
        Assert.ThrowsExactly<ArgumentException>(() => LoadMachO("unixthread_i386").AdHocSign(string.Empty));
    }

    private static Span<byte> FindCodeDirectory(byte[] image, uint signatureOffset)
        => FindBlob(image, signatureOffset, CodeDirectorySlot);

    private static Span<byte> FindBlob(byte[] image, uint signatureOffset, uint slot)
    {
        var superBlob = image.AsSpan((int)signatureOffset);
        var count = BinaryPrimitives.ReadUInt32BigEndian(superBlob.Slice(8));
        for (var i = 0; i < count; i++)
        {
            var entry = superBlob.Slice(12 + i * 8);
            if (BinaryPrimitives.ReadUInt32BigEndian(entry) != slot) continue;

            var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(4));
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(superBlob.Slice(offset + 4));
            return superBlob.Slice(offset, length);
        }

        throw new InvalidOperationException($"The signature has no blob in slot {slot}");
    }
}
