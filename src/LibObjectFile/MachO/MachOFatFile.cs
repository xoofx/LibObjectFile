// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using LibObjectFile.Diagnostics;
using LibObjectFile.IO;
using LibObjectFile.MachO.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.MachO;

/// <summary>
/// A universal binary, holding one <see cref="MachOFile"/> per architecture.
/// </summary>
/// <remarks>
/// The header and its slice table are stored big-endian whatever the architectures inside are,
/// which is the one place the format does not follow the image's own byte order.
/// <para>
/// Slices are aligned to the page size of their architecture so the loader can map one straight
/// out of the containing file, which is why a universal binary is larger than its slices put
/// together.
/// </para>
/// </remarks>
public sealed partial class MachOFatFile
{
    /// <summary>
    /// The size of the header preceding the slice table (<c>fat_header</c>).
    /// </summary>
    public static unsafe int HeaderSize => sizeof(RawFatHeader);

    /// <summary>
    /// Gets the size of one slice table entry for the given offset width.
    /// </summary>
    /// <param name="is64BitOffsets">Whether slice offsets are stored as 64-bit values.</param>
    /// <returns>The size of a <c>fat_arch_64</c> or a <c>fat_arch</c>.</returns>
    public static unsafe int GetSliceEntrySize(bool is64BitOffsets)
        => is64BitOffsets ? sizeof(RawFatArch64) : sizeof(RawFatArch);

    /// <summary>
    /// Gets the slices of this universal binary, in the order the header lists them.
    /// </summary>
    public List<MachOFatSlice> Slices { get; } = [];

    /// <summary>
    /// Gets or sets whether slice offsets are stored as 64-bit values, which is needed once a
    /// slice starts beyond 4GB.
    /// </summary>
    public bool Is64BitOffsets { get; set; }

    /// <summary>
    /// Checks whether a stream starts with a universal binary magic, without consuming it.
    /// </summary>
    /// <param name="stream">The stream to inspect.</param>
    /// <returns><c>true</c> if the stream starts with a universal binary magic.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static bool IsFat(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var position = stream.Position;
        try
        {
            Span<byte> magic = stackalloc byte[4];
            if (stream.Read(magic) != 4) return false;
            var value = BinaryPrimitives.ReadUInt32BigEndian(magic);
            return value is MachOMagic.FatMagic or MachOMagic.FatMagic64;
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <summary>
    /// Reads a universal binary from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the file.</param>
    /// <param name="options">Options controlling how each slice is read, or null for the defaults.</param>
    /// <returns>The universal binary read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ObjectFileException">The stream does not contain a readable universal binary.</exception>
    public static MachOFatFile Read(Stream stream, MachOReaderOptions? options = null)
    {
        if (!TryRead(stream, out var file, out var diagnostics, options))
        {
            throw new ObjectFileException($"Unexpected error while reading the Mach-O universal binary", diagnostics);
        }
        return file;
    }

    /// <summary>
    /// Tries to read a universal binary from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the file.</param>
    /// <param name="file">The universal binary read, if reading succeeded.</param>
    /// <param name="diagnostics">The diagnostics collected, if reading failed.</param>
    /// <param name="options">Options controlling how each slice is read, or null for the defaults.</param>
    /// <returns><c>true</c> if the file was read; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static bool TryRead(Stream stream, [NotNullWhen(true)] out MachOFatFile? file, [NotNullWhen(false)] out DiagnosticBag? diagnostics, MachOReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var bag = new DiagnosticBag();
        file = new MachOFatFile();
        diagnostics = bag;

        var basePosition = stream.Position;
        Span<byte> header = stackalloc byte[HeaderSize];
        stream.Position = basePosition;
        if (stream.Read(header) != HeaderSize)
        {
            bag.Error(DiagnosticId.MACHO_ERR_InvalidFatHeader, "The stream is too short to hold a universal binary header");
            file = null;
            return false;
        }

        var magic = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (magic is not (MachOMagic.FatMagic or MachOMagic.FatMagic64))
        {
            bag.Error(DiagnosticId.MACHO_ERR_InvalidFatHeader, $"Invalid universal binary magic 0x{magic:X8}");
            file = null;
            return false;
        }

        file.Is64BitOffsets = magic == MachOMagic.FatMagic64;
        var count = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(4));

        var entrySize = GetSliceEntrySize(file.Is64BitOffsets);
        if ((ulong)count * (ulong)entrySize > (ulong)(stream.Length - stream.Position))
        {
            bag.Error(DiagnosticId.MACHO_ERR_InvalidFatHeader, $"The universal binary header claims {count} architectures, which do not fit in the file");
            file = null;
            return false;
        }

        var entries = new byte[count * entrySize];
        try
        {
            stream.ReadExactly(entries);
        }
        catch (EndOfStreamException)
        {
            bag.Error(DiagnosticId.MACHO_ERR_InvalidFatHeader, $"The universal binary header claims {count} architectures, which do not fit in the file");
            file = null;
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var entry = entries.AsSpan(i * entrySize);
            var slice = new MachOFatSlice
            {
                CpuType = (MachOCpuType)BinaryPrimitives.ReadUInt32BigEndian(entry),
                CpuSubType = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(4)),
            };

            if (file.Is64BitOffsets)
            {
                slice.FileOffset = BinaryPrimitives.ReadUInt64BigEndian(entry.Slice(8));
                slice.Size = BinaryPrimitives.ReadUInt64BigEndian(entry.Slice(16));
                slice.AlignLog2 = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(24));
            }
            else
            {
                slice.FileOffset = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(8));
                slice.Size = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(12));
                slice.AlignLog2 = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(16));
            }

            if (slice.AlignLog2 > MachOFatSlice.MaxAlignLog2)
            {
                bag.Error(DiagnosticId.MACHO_ERR_InvalidFatSliceAlignment, $"Slice {i} has an alignment exponent of {slice.AlignLog2}, past the {MachOFatSlice.MaxAlignLog2} a universal binary allows");
                file = null;
                return false;
            }

            if (slice.Size < MachOFile.MinHeaderSize)
            {
                bag.Error(DiagnosticId.MACHO_ERR_InvalidFatArchRange, $"Slice {i} is {slice.Size} bytes, too short to hold a Mach-O header");
                file = null;
                return false;
            }

            if (slice.FileOffset + slice.Size > (ulong)stream.Length)
            {
                bag.Error(DiagnosticId.MACHO_ERR_InvalidFatArchRange, $"Slice {i} spans [0x{slice.FileOffset:X}, 0x{slice.FileOffset + slice.Size:X}) which extends past the end of the file");
                file = null;
                return false;
            }

            // Each slice is a complete image, so it is read through a view bounded to the slice.
            // Handing it the whole stream would let a malformed slice read its neighbours.
            var sliceStream = new SubStream(stream, basePosition + (long)slice.FileOffset, (long)slice.Size);
            if (!MachOFile.TryRead(sliceStream, out var sliceFile, out var sliceDiagnostics, options))
            {
                foreach (var message in sliceDiagnostics.Messages)
                {
                    bag.Log(message);
                }
                file = null;
                return false;
            }

            slice.File = sliceFile;
            file.Slices.Add(slice);
        }

        diagnostics = null;
        return true;
    }

    /// <summary>
    /// Writes this universal binary to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ObjectFileException">The universal binary or one of its slices is not writable.</exception>
    /// <remarks>
    /// The stream is grown to hold the last slice if it is shorter, and is flushed before this
    /// method returns but is not disposed.
    /// </remarks>
    public void Write(Stream stream)
    {
        if (!TryWrite(stream, out var diagnostics))
        {
            throw new ObjectFileException($"Unexpected error while writing the Mach-O universal binary", diagnostics);
        }
    }

    /// <summary>
    /// Tries to write this universal binary to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="diagnostics">The diagnostics collected while writing.</param>
    /// <returns><c>true</c> if the file was written without errors; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <remarks>
    /// Each slice is laid out before the slices are placed, so a slice that changed size since it
    /// was read is recorded at the size it actually writes rather than overrunning the one after
    /// it. The space between slices is left zeroed: every universal binary examined pads with
    /// zeros, and slices are aligned rather than packed, so the gaps hold nothing worth
    /// preserving.
    /// <para>
    /// The stream is grown to hold the last slice if it is shorter, and is flushed before this
    /// method returns but is not disposed.
    /// </para>
    /// </remarks>
    public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(stream);

        diagnostics = new DiagnosticBag();

        // Each slice is verified and laid out before the containing file is, because the size
        // recorded for a slice has to be the size that slice goes on to write.
        foreach (var slice in Slices)
        {
            if (slice.File is null) continue;

            var context = new MachOVisitorContext(slice.File, diagnostics);
            slice.File.Verify(context);
            if (diagnostics.HasErrors) return false;

            slice.File.UpdateLayout(context);
            if (diagnostics.HasErrors) return false;
        }

        UpdateLayout();

        Verify(diagnostics);
        if (diagnostics.HasErrors) return false;

        var basePosition = stream.Position;
        var entrySize = GetSliceEntrySize(Is64BitOffsets);

        var header = new byte[HeaderSize + Slices.Count * entrySize];
        BinaryPrimitives.WriteUInt32BigEndian(header, Is64BitOffsets ? MachOMagic.FatMagic64 : MachOMagic.FatMagic);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)Slices.Count);

        for (var i = 0; i < Slices.Count; i++)
        {
            var slice = Slices[i];
            var entry = header.AsSpan(HeaderSize + i * entrySize);
            BinaryPrimitives.WriteUInt32BigEndian(entry, (uint)slice.CpuType);
            BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(4), slice.CpuSubType);

            if (Is64BitOffsets)
            {
                BinaryPrimitives.WriteUInt64BigEndian(entry.Slice(8), slice.FileOffset);
                BinaryPrimitives.WriteUInt64BigEndian(entry.Slice(16), slice.Size);
                BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(24), slice.AlignLog2);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(8), (uint)slice.FileOffset);
                BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(12), (uint)slice.Size);
                BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(16), slice.AlignLog2);
            }
        }

        stream.Write(header);

        foreach (var slice in Slices)
        {
            if (slice.File is null) continue;
            stream.Position = basePosition + (long)slice.FileOffset;
            var writer = new MachOWriter(slice.File, stream, diagnostics);
            slice.File.Write(writer);
            if (diagnostics.HasErrors) return false;
        }

        var end = Slices.Count == 0 ? 0 : Slices.Max(s => (long)(s.FileOffset + s.Size));
        if (stream.Length < basePosition + end)
        {
            stream.SetLength(basePosition + end);
        }

        stream.Flush();

        return !diagnostics.HasErrors;
    }

    /// <summary>
    /// Recomputes where each slice sits and how large it is.
    /// </summary>
    /// <remarks>
    /// A slice is mapped straight out of the containing file, so it has to start on a page
    /// boundary for its architecture. Sizes are computed from each slice rather than taken from
    /// what it was read as, since an edited slice no longer matches that, and the header would
    /// otherwise point the loader at a slice that overruns the one after it.
    /// </remarks>
    public void UpdateLayout()
    {
        var cursor = (ulong)(HeaderSize + Slices.Count * GetSliceEntrySize(Is64BitOffsets));

        foreach (var slice in Slices)
        {
            if (slice.File is null)
            {
                slice.Size = 0;
                continue;
            }

            var alignment = slice.Alignment;
            slice.FileOffset = AlignHelper.AlignUp(cursor, alignment);
            slice.Size = slice.File.ComputeFileSize();
            cursor = slice.FileOffset + slice.Size;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(MachOFatFile)} {{ Slices = {Slices.Count} }}";
}
