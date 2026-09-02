// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Reads a Mach-O image from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the image.</param>
    /// <param name="options">Options controlling how content is read, or null for the defaults.</param>
    /// <returns>The image read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ObjectFileException">The stream does not contain a readable Mach-O image.</exception>
    public static MachOFile Read(Stream stream, MachOReaderOptions? options = null)
    {
        if (!TryRead(stream, out var file, out var diagnostics, options))
        {
            throw new ObjectFileException($"Unexpected error while reading the Mach-O file", diagnostics);
        }
        return file;
    }

    /// <summary>
    /// Tries to read a Mach-O image from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the image.</param>
    /// <param name="file">The image read, if reading succeeded.</param>
    /// <param name="diagnostics">The diagnostics collected, if reading failed.</param>
    /// <param name="options">Options controlling how content is read, or null for the defaults.</param>
    /// <returns><c>true</c> if the image was read; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static bool TryRead(Stream stream, [NotNullWhen(true)] out MachOFile? file, [NotNullWhen(false)] out DiagnosticBag? diagnostics, MachOReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        file = new MachOFile();
        var reader = new MachOReader(file, stream, options ?? new MachOReaderOptions());
        diagnostics = reader.Diagnostics;

        file.Read(reader);

        if (reader.Diagnostics.HasErrors)
        {
            file = null;
            return false;
        }

        diagnostics = null;
        return true;
    }

    /// <summary>
    /// Checks whether a stream starts with a thin Mach-O magic, without consuming it.
    /// </summary>
    /// <param name="stream">The stream to inspect.</param>
    /// <returns><c>true</c> if the stream starts with a Mach-O magic.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static bool IsMachO(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var position = stream.Position;
        try
        {
            Span<byte> magic = stackalloc byte[4];
            if (stream.Read(magic) != 4) return false;
            var value = BitConverter.ToUInt32(magic);
            return value is MachOMagic.Magic32 or MachOMagic.Magic64 or MachOMagic.Cigam32 or MachOMagic.Cigam64;
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        Position = reader.Position;

        var magic = reader.ReadU32();
        reader.Position -= 4;

        switch (magic)
        {
            case MachOMagic.Magic32:
                Is64Bit = false;
                break;
            case MachOMagic.Magic64:
                Is64Bit = true;
                break;
            case MachOMagic.Cigam32:
            case MachOMagic.Cigam64:
                // Reading these means byte-swapping every field. The only architectures that
                // shipped big-endian Mach-O are PowerPC and 68k, so nothing is done here rather
                // than carrying a swapping reader that nothing exercises.
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_UnsupportedByteOrder, $"Big-endian Mach-O images are not supported (magic 0x{magic:X8})");
                return;
            case MachOMagic.FatCigam:
            case MachOMagic.FatCigam64:
                // The fat magics are stored big-endian, so a little-endian read of a well-formed
                // universal binary sees the swapped spelling.
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_UnexpectedFatFile, "This is a universal binary rather than a single Mach-O image. Read it with MachOFatFile and pick a slice.");
                return;
            default:
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_InvalidMagic, $"Invalid Mach-O magic 0x{magic:X8}");
                return;
        }

        uint numberOfCommands;
        uint sizeOfCommands;
        if (Is64Bit)
        {
            if (!reader.TryReadData(sizeof(RawMachHeader64), out RawMachHeader64 header))
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_InvalidMagic, "Truncated 64-bit Mach-O header");
                return;
            }
            CpuType = (MachOCpuType)header.CpuType;
            CpuSubType = header.CpuSubType;
            FileType = (MachOFileType)header.FileType;
            Flags = (MachOHeaderFlags)header.Flags;
            Reserved = header.Reserved;
            numberOfCommands = header.NumberOfCommands;
            sizeOfCommands = header.SizeOfCommands;
        }
        else
        {
            if (!reader.TryReadData(sizeof(RawMachHeader32), out RawMachHeader32 header))
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_InvalidMagic, "Truncated 32-bit Mach-O header");
                return;
            }
            CpuType = (MachOCpuType)header.CpuType;
            CpuSubType = header.CpuSubType;
            FileType = (MachOFileType)header.FileType;
            Flags = (MachOHeaderFlags)header.Flags;
            numberOfCommands = header.NumberOfCommands;
            sizeOfCommands = header.SizeOfCommands;
        }

        ReadLoadCommands(reader, numberOfCommands, sizeOfCommands);
        if (reader.Diagnostics.HasErrors) return;

        ReadContent(reader);
    }

    /// <summary>
    /// Walks the load command table. The table is bounded by the <c>sizeofcmds</c> the header
    /// declares, each command by its own <c>cmdsize</c>, so a command cannot reach into the one
    /// after it or into the content beyond the table.
    /// </summary>
    private void ReadLoadCommands(MachOReader reader, uint numberOfCommands, uint sizeOfCommands)
    {
        var commandAlignment = MachOLoadCommand.GetSizeAlignment(Is64Bit);
        var tableEnd = HeaderSize + (ulong)sizeOfCommands;

        if (tableEnd > reader.Length)
        {
            reader.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_TruncatedLoadCommand,
                $"The header declares {sizeOfCommands} bytes of load commands, which extend past the end of the file");
            return;
        }

        for (uint i = 0; i < numberOfCommands; i++)
        {
            var commandPosition = reader.Position;
            if (commandPosition + 8 > tableEnd)
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Load command {i} starts past the end of the load command table");
                return;
            }

            var type = (MachOLoadCommandType)reader.ReadU32();
            var commandSize = reader.ReadU32();
            reader.Position = commandPosition;

            if (commandSize < 8 || (commandSize % commandAlignment) != 0)
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_InvalidLoadCommandSize, $"Load command {i} has an invalid cmdsize of {commandSize}, which must be at least 8 and a multiple of {commandAlignment}");
                return;
            }

            if (commandPosition + commandSize > tableEnd)
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Load command {i} of size {commandSize} extends past the end of the load command table");
                return;
            }

            MachOLoadCommand command = type switch
            {
                MachOLoadCommandType.Segment => new MachOSegment { Is64Bit = false },
                MachOLoadCommandType.Segment64 => new MachOSegment { Is64Bit = true },
                MachOLoadCommandType.LoadDylib
                    or MachOLoadCommandType.IdDylib
                    or MachOLoadCommandType.LoadWeakDylib
                    or MachOLoadCommandType.ReexportDylib
                    or MachOLoadCommandType.LoadUpwardDylib
                    or MachOLoadCommandType.LazyLoadDylib => new MachODylibCommand { Is64Bit = Is64Bit },
                MachOLoadCommandType.RPath
                    or MachOLoadCommandType.LoadDylinker
                    or MachOLoadCommandType.IdDylinker
                    or MachOLoadCommandType.DyldEnvironment => new MachOPathCommand { Is64Bit = Is64Bit },
                MachOLoadCommandType.SymbolTable => new MachOSymbolTableCommand(),
                MachOLoadCommandType.TwoLevelHints => new MachOTwoLevelHintsCommand(),
                MachOLoadCommandType.Thread or MachOLoadCommandType.UnixThread => new MachOThreadCommand(),
                MachOLoadCommandType.Main => new MachOMainCommand(),
                MachOLoadCommandType.Uuid => new MachOUuidCommand(),
                MachOLoadCommandType.BuildVersion => new MachOBuildVersionCommand(),
                MachOLoadCommandType.SourceVersion => new MachOSourceVersionCommand(),
                MachOLoadCommandType.VersionMinMacOSX
                    or MachOLoadCommandType.VersionMinIPhoneOS
                    or MachOLoadCommandType.VersionMinTvOS
                    or MachOLoadCommandType.VersionMinWatchOS => new MachOVersionMinCommand(),
                MachOLoadCommandType.DyldInfo or MachOLoadCommandType.DyldInfoOnly => new MachODyldInfoCommand(),
                MachOLoadCommandType.DynamicSymbolTable => new MachODynamicSymbolTableCommand(),
                MachOLoadCommandType.CodeSignature
                    or MachOLoadCommandType.FunctionStarts
                    or MachOLoadCommandType.DataInCode
                    or MachOLoadCommandType.DyldExportsTrie
                    or MachOLoadCommandType.DyldChainedFixups
                    or MachOLoadCommandType.SegmentSplitInfo
                    or MachOLoadCommandType.DylibCodeSignDrs
                    or MachOLoadCommandType.LinkerOptimizationHint => new MachOLinkEditDataCommand(),
                _ => new MachOUnknownLoadCommand(),
            };

            command.Type = type;
            command.Position = commandPosition;
            command.Size = commandSize;

            if (commandSize < command.MinimumCommandSize)
            {
                reader.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidLoadCommandSize,
                    $"Load command {i} of type {type} has a cmdsize of {commandSize}, which is smaller than the {command.MinimumCommandSize} bytes its fixed part needs");
                return;
            }

            LoadCommands.Add(command);

            command.Read(reader);
            if (reader.Diagnostics.HasErrors) return;

            // A command that read past its own cmdsize took bytes belonging to the next one, so
            // whatever it decoded is not what the file says.
            if (reader.Position > commandPosition + commandSize)
            {
                reader.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_LoadCommandOverread,
                    $"Load command {i} of type {type} read to 0x{reader.Position:X}, past the 0x{commandPosition + commandSize:X} its cmdsize allows");
                return;
            }

            reader.Position = commandPosition + commandSize;
        }

        if (reader.Position != tableEnd)
        {
            reader.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_LoadCommandTableSizeMismatch,
                $"The load commands end at 0x{reader.Position:X} but the header declares the table ends at 0x{tableEnd:X}");
        }
    }

    /// <summary>
    /// Turns the rest of the file into content, so that every byte belongs to something. The
    /// header and the load command table come first, then whatever the commands point at, and
    /// the gaps between them are kept as they are rather than regenerated.
    /// </summary>
    private void ReadContent(MachOReader reader)
    {
        var commandsEnd = (ulong)LoadCommandsEndOffset;
        var fileLength = reader.Length;

        Content.Add(new MachOHeaderContent { Position = 0, Size = HeaderSize });
        Content.Add(new MachOLoadCommandTable { Position = HeaderSize, Size = SizeOfCommands });

        var regions = CollectKnownRegions(reader, commandsEnd, fileLength);
        if (reader.Diagnostics.HasErrors) return;

        regions.Sort((left, right) => left.Offset.CompareTo(right.Offset));

        var cursor = commandsEnd;
        var firstGap = true;

        foreach (var region in regions)
        {
            // Overlapping regions would mean the same bytes belong to two things; keep the first.
            if (region.Offset < cursor) continue;

            if (region.Offset > cursor)
            {
                var gap = AddStreamContent(reader, cursor, region.Offset - cursor, firstGap);
                if (firstGap)
                {
                    LoadCommandPadding = gap;
                    firstGap = false;
                }
            }

            reader.Position = region.Offset;
            var content = region.Section is null
                ? new MachOStreamContent(reader.ReadAsStream(region.Size))
                : new MachOSectionData(region.Section, reader.ReadAsStream(region.Size));
            content.Position = region.Offset;
            Content.Add(content);
            cursor = region.Offset + region.Size;
            firstGap = false;
        }

        if (cursor < fileLength)
        {
            var trailing = AddStreamContent(reader, cursor, fileLength - cursor, firstGap);
            if (firstGap) LoadCommandPadding = trailing;
        }
    }

    /// <summary>
    /// A run of bytes something in the image points at.
    /// </summary>
    private readonly record struct KnownRegion(ulong Offset, ulong Size, MachOSection? Section);

    /// <summary>
    /// Finds every run of bytes the header or a load command points at. What is left over between
    /// them is padding, and is kept verbatim.
    /// </summary>
    private List<KnownRegion> CollectKnownRegions(MachOReader reader, ulong commandsEnd, ulong fileLength)
    {
        var regions = new List<KnownRegion>();

        // Sizes are counts from the file times an entry size, computed wide: a count large enough
        // to wrap a 32-bit product would otherwise name a small region and leave the rest of the
        // table looking like padding.
        void Add(ulong offset, ulong size, MachOSection? section = null)
        {
            // An offset of zero means the data is absent rather than at the start of the file.
            if (offset < commandsEnd || size == 0) return;

            if (offset + size > fileLength)
            {
                reader.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidSectionFileRange,
                    $"Content at 0x{offset:X} for 0x{size:X} bytes extends past the end of the file");
                return;
            }

            regions.Add(new KnownRegion(offset, size, section));
        }

        foreach (var segment in Segments)
        {
            foreach (var section in segment.Sections)
            {
                if (!section.IsZeroFill)
                {
                    Add(section.FileOffset, section.Size, section);
                }

                Add(section.RelocationOffset, (ulong)section.NumberOfRelocations * MachORelocation.EntrySize);
            }
        }

        foreach (var command in LoadCommands)
        {
            switch (command)
            {
                case MachOSymbolTableCommand symtab:
                    Add(symtab.SymbolOffset, symtab.SymbolCount * (ulong)MachOSymbolTableCommand.GetSymbolSize(Is64Bit));
                    Add(symtab.StringOffset, symtab.StringSize);
                    break;
                case MachODynamicSymbolTableCommand dysymtab:
                    Add(dysymtab.TableOfContentsOffset, (ulong)dysymtab.TableOfContentsCount * MachODynamicSymbolTableCommand.TableOfContentsEntrySize);
                    Add(dysymtab.ModuleTableOffset, (ulong)dysymtab.ModuleTableCount * MachODynamicSymbolTableCommand.GetModuleTableEntrySize(Is64Bit));
                    Add(dysymtab.ExternalReferenceOffset, (ulong)dysymtab.ExternalReferenceCount * MachODynamicSymbolTableCommand.ExternalReferenceEntrySize);
                    Add(dysymtab.IndirectSymbolOffset, (ulong)dysymtab.IndirectSymbolCount * MachODynamicSymbolTableCommand.IndirectSymbolEntrySize);
                    Add(dysymtab.ExternalRelocationOffset, (ulong)dysymtab.ExternalRelocationCount * MachORelocation.EntrySize);
                    Add(dysymtab.LocalRelocationOffset, (ulong)dysymtab.LocalRelocationCount * MachORelocation.EntrySize);
                    break;
                case MachODyldInfoCommand dyldInfo:
                    Add(dyldInfo.RebaseOffset, dyldInfo.RebaseSize);
                    Add(dyldInfo.BindOffset, dyldInfo.BindSize);
                    Add(dyldInfo.WeakBindOffset, dyldInfo.WeakBindSize);
                    Add(dyldInfo.LazyBindOffset, dyldInfo.LazyBindSize);
                    Add(dyldInfo.ExportOffset, dyldInfo.ExportSize);
                    break;
                case MachOLinkEditDataCommand data:
                    Add(data.DataOffset, data.DataSize);
                    break;
                case MachOTwoLevelHintsCommand hints:
                    Add(hints.Offset, (ulong)hints.HintCount * MachOTwoLevelHintsCommand.HintSize);
                    break;
            }
        }

        return regions;
    }

    private MachOStreamContent AddStreamContent(MachOReader reader, ulong offset, ulong size, bool isLoadCommandPadding)
    {
        reader.Position = offset;
        var stream = reader.ReadAsStream(size);
        MachOStreamContent content = isLoadCommandPadding
            ? new MachOLoadCommandPadding(stream)
            : new MachOStreamContent(stream);
        content.Position = offset;
        Content.Add(content);
        return content;
    }
}
