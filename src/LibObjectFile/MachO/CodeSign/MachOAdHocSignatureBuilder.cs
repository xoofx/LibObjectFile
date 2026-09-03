// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile.MachO.CodeSign;

using static MachOCodeSignatureConstants;

/// <summary>
/// Builds an ad-hoc embedded code signature.
/// </summary>
/// <remarks>
/// An ad-hoc signature carries no certificate. It states only that the image hashes to what the
/// code directory says, which is what lets the kernel give the process a stable identity. Apple
/// Silicon refuses to execute an unsigned image at all, so this is a requirement there rather
/// than a hardening measure.
/// </remarks>
public sealed class MachOAdHocSignatureBuilder
{
    private readonly byte[] _identifier;

    /// <summary>
    /// Initializes a builder for an image identified by <paramref name="identifier"/>.
    /// </summary>
    /// <param name="identifier">
    /// The signing identity recorded in the code directory. Apple's tooling uses the file name
    /// of the binary, and any process asking for this image's identity gets this string back.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    public MachOAdHocSignatureBuilder(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        Identifier = identifier;
        var bytes = Encoding.UTF8.GetBytes(identifier);
        _identifier = new byte[bytes.Length + 1];
        bytes.CopyTo(_identifier, 0);
    }

    /// <summary>
    /// Gets the signing identity recorded in the code directory.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets or sets the number of bytes of the image covered by the signature, which is the file
    /// offset the signature itself starts at.
    /// </summary>
    public uint CodeLimit { get; set; }

    /// <summary>
    /// Gets or sets the file offset of the executable segment, normally <c>__TEXT</c>.
    /// </summary>
    public ulong ExecSegmentBase { get; set; }

    /// <summary>
    /// Gets or sets the file size of the executable segment.
    /// </summary>
    public ulong ExecSegmentLimit { get; set; }

    /// <summary>
    /// Gets or sets the executable segment flags, which mark whether the image is a main binary.
    /// </summary>
    public ulong ExecSegmentFlags { get; set; }

    /// <summary>
    /// Gets the number of page hashes the code directory will hold for the current <see cref="CodeLimit"/>.
    /// </summary>
    public uint CodeSlotCount => (uint)(((ulong)CodeLimit + PageSize - 1) / PageSize);

    /// <summary>
    /// Gets the total size of the signature for the current <see cref="CodeLimit"/> and identifier.
    /// </summary>
    /// <remarks>
    /// The size depends only on those two, so a caller can reserve room for the signature and
    /// lay out the image before any hash has been computed.
    /// </remarks>
    public uint ComputeSize()
        => AlignHelper.AlignUp((uint)ContentSize, (uint)SignatureAlignment);

    /// <summary>
    /// The size of the blobs themselves, which is what the SuperBlob header records. The room
    /// reserved for the signature is rounded up from this, and the padding is not part of the
    /// SuperBlob: codesign reports the length field as the blob's own, and Apple's ad-hoc output
    /// leaves the slack to the load command's datasize.
    /// </summary>
    private int ContentSize => SuperBlobSize + CodeDirectorySize + RequirementsSize + BlobWrapperSize;

    // Header plus one index entry for each of the code directory, requirements and CMS slots.
    private const int SuperBlobSize = 12 + 3 * 8;
    private const int RequirementsSize = 12;
    private const int BlobWrapperSize = 8;

    // Two special slots are described: the Info.plist hash and the requirements hash.
    private const int SpecialSlotCount = 2;

    private int HashOffset => CodeDirectoryHeaderSize + _identifier.Length + SpecialSlotCount * Sha256Size;

    private int CodeDirectorySize => HashOffset + (int)CodeSlotCount * Sha256Size;

    /// <summary>
    /// Produces the signature for an image whose first <see cref="CodeLimit"/> bytes are
    /// <paramref name="image"/>.
    /// </summary>
    /// <param name="image">
    /// The image content up to <see cref="CodeLimit"/>. The signature is not part of what it
    /// covers, so this excludes the signature itself.
    /// </param>
    /// <returns>The signature, of exactly <see cref="ComputeSize"/> bytes.</returns>
    /// <exception cref="ArgumentException"><paramref name="image"/> is shorter than <see cref="CodeLimit"/>.</exception>
    public byte[] Build(ReadOnlySpan<byte> image)
    {
        if (image.Length < CodeLimit)
        {
            throw new ArgumentException($"The image is {image.Length} bytes but the signature covers {CodeLimit}", nameof(image));
        }

        var result = new byte[ComputeSize()];
        var span = result.AsSpan();

        var codeDirectoryOffset = SuperBlobSize;
        var requirementsOffset = codeDirectoryOffset + CodeDirectorySize;
        var blobWrapperOffset = requirementsOffset + RequirementsSize;

        WriteRequirements(span.Slice(requirementsOffset, RequirementsSize));
        WriteBlobWrapper(span.Slice(blobWrapperOffset, BlobWrapperSize));
        WriteCodeDirectory(span.Slice(codeDirectoryOffset, CodeDirectorySize), image, span.Slice(requirementsOffset, RequirementsSize));
        WriteSuperBlob(span, codeDirectoryOffset, requirementsOffset, blobWrapperOffset, ContentSize);

        return result;
    }

    private static void WriteSuperBlob(Span<byte> span, int codeDirectoryOffset, int requirementsOffset, int blobWrapperOffset, int totalLength)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, EmbeddedSignatureMagic);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), (uint)totalLength);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), 3);

        WriteIndexEntry(span.Slice(12), CodeDirectorySlot, codeDirectoryOffset);
        WriteIndexEntry(span.Slice(20), RequirementsSlot, requirementsOffset);
        WriteIndexEntry(span.Slice(28), SignatureSlot, blobWrapperOffset);
    }

    private static void WriteIndexEntry(Span<byte> span, uint slot, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, slot);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), (uint)offset);
    }

    /// <summary>
    /// Writes an empty requirement set. Ad-hoc signing states no requirements, but the slot is
    /// still present and hashed, because its absence is not the same as it being empty.
    /// </summary>
    private static void WriteRequirements(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, RequirementsMagic);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), RequirementsSize);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), 0);
    }

    /// <summary>
    /// Writes the empty wrapper that would hold a CMS signature.
    /// </summary>
    /// <remarks>
    /// The slot is present and empty rather than omitted, which is what <c>codesign</c> produces
    /// when it signs ad-hoc: a shipping ad-hoc signed dylib has exactly these three blobs, a code
    /// directory, a twelve byte empty requirement set and this eight byte wrapper. A linker
    /// writes something smaller still, a lone code directory flagged <c>LINKER_SIGNED</c>, but
    /// that is a different thing from what signing a finished image produces.
    /// </remarks>
    private static void WriteBlobWrapper(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, BlobWrapperMagic);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), BlobWrapperSize);
    }

    private void WriteCodeDirectory(Span<byte> span, ReadOnlySpan<byte> image, ReadOnlySpan<byte> requirements)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, CodeDirectoryMagic);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), (uint)CodeDirectorySize);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), CodeDirectoryVersionExecSeg);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(12), AdHocFlag);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(16), (uint)HashOffset);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(20), CodeDirectoryHeaderSize);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(24), SpecialSlotCount);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(28), CodeSlotCount);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(32), CodeLimit);
        span[36] = Sha256Size;
        span[37] = HashTypeSha256;
        span[38] = 0;
        span[39] = PageSizeLog2;
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(40), 0);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(44), 0);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(48), 0);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(52), 0);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(56), 0);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(64), ExecSegmentBase);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(72), ExecSegmentLimit);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(80), ExecSegmentFlags);

        _identifier.CopyTo(span.Slice(CodeDirectoryHeaderSize));

        // Special slots are indexed backwards from the code hashes, so slot n sits n hashes
        // before the start of the code hashes. Slot 1 is the Info.plist, which a bare executable
        // does not have, and a slot with nothing in it is recorded as zero rather than omitted.
        var hashOffset = HashOffset;
        span.Slice(hashOffset - SpecialSlotCount * Sha256Size, SpecialSlotCount * Sha256Size).Clear();
        SHA256.HashData(requirements, span.Slice(hashOffset - (int)RequirementsSlot * Sha256Size, Sha256Size));

        for (var slot = 0; slot < CodeSlotCount; slot++)
        {
            var start = slot * PageSize;
            var length = Math.Min(PageSize, (int)CodeLimit - start);
            SHA256.HashData(image.Slice(start, length), span.Slice(hashOffset + slot * Sha256Size, Sha256Size));
        }
    }

}
