// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

/// <summary>
/// A Mach-O image that can be read, modified and written back.
/// </summary>
/// <remarks>
/// The writer places content at the offsets recorded in the load commands rather than computing
/// a fresh layout. Every byte outside the header and the load command table keeps its position,
/// so addresses, relocations and code stay valid across a read-modify-write cycle. That is what
/// makes in-place load command injection and code signing possible on an existing image, and it
/// is also why growing the load command table is bounded by
/// <see cref="AvailableLoadCommandSpace"/> rather than free.
/// </remarks>
public sealed partial class MachOFile : MachOObject
{
    private readonly ObjectList<MachOLoadCommand> _loadCommands;
    private readonly ObjectList<MachOContent> _content;

    /// <summary>
    /// Initializes a new empty instance.
    /// </summary>
    public MachOFile()
    {
        _loadCommands = new ObjectList<MachOLoadCommand>(this);
        _content = new ObjectList<MachOContent>(this);
    }

    /// <summary>
    /// Gets or sets whether this image uses the 64-bit layout.
    /// </summary>
    public bool Is64Bit { get; set; }

    /// <summary>
    /// Gets or sets the CPU architecture of this image.
    /// </summary>
    public MachOCpuType CpuType { get; set; }

    /// <summary>
    /// Gets or sets the CPU subtype. Its meaning depends on <see cref="CpuType"/>, so it is kept
    /// as the raw value rather than being interpreted.
    /// </summary>
    public uint CpuSubType { get; set; }

    /// <summary>
    /// Gets or sets the kind of image.
    /// </summary>
    public MachOFileType FileType { get; set; }

    /// <summary>
    /// Gets or sets the header flags.
    /// </summary>
    public MachOHeaderFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets the reserved header field, present only in 64-bit images.
    /// </summary>
    public uint Reserved { get; set; }

    /// <summary>
    /// Gets the load commands of this image, in file order. The order is meaningful: dyld
    /// identifies a library by the position of its command among the others.
    /// </summary>
    public ObjectList<MachOLoadCommand> LoadCommands => _loadCommands;

    /// <summary>
    /// Gets everything in this image, in file order: the header, the load command table, each
    /// section's bytes, each table in <c>__LINKEDIT</c>, and the padding between them.
    /// </summary>
    /// <remarks>
    /// Every byte of the file belongs to one of these, so writing the list back out reproduces
    /// the image and a layout is a single walk over it.
    /// </remarks>
    public ObjectList<MachOContent> Content => _content;

    /// <summary>
    /// Gets the segments of this image, in load command order.
    /// </summary>
    public IEnumerable<MachOSegment> Segments => LoadCommands.OfType<MachOSegment>();

    /// <summary>
    /// Gets the libraries this image links against, in the order dyld assigns their ordinals.
    /// </summary>
    public IEnumerable<MachODylibCommand> LinkedLibraries
        => LoadCommands.OfType<MachODylibCommand>().Where(c => c.Type != MachOLoadCommandType.IdDylib);

    /// <summary>
    /// Gets the runpaths <c>@rpath</c> expands to, in search order.
    /// </summary>
    public IEnumerable<MachOPathCommand> RunPaths
        => LoadCommands.OfType<MachOPathCommand>().Where(c => c.Type == MachOLoadCommandType.RPath);

    /// <summary>
    /// Gets the command giving this image's own install name, present only in a dylib.
    /// </summary>
    public MachODylibCommand? IdDylib
        => LoadCommands.OfType<MachODylibCommand>().FirstOrDefault(c => c.Type == MachOLoadCommandType.IdDylib);

    /// <summary>
    /// Gets the size of the Mach-O header, which is 32 bytes for a 64-bit image and 28 otherwise.
    /// </summary>
    public uint HeaderSize => Is64Bit ? 32u : 28u;

    /// <summary>
    /// The smallest a Mach-O header can be, which is the 32-bit one. Nothing shorter than this
    /// can hold an image.
    /// </summary>
    internal const uint MinHeaderSize = 28;

    /// <summary>
    /// Gets the total size of the load commands, the value stored in <c>sizeofcmds</c>.
    /// </summary>
    public uint SizeOfCommands
    {
        get
        {
            uint total = 0;
            foreach (var command in LoadCommands)
            {
                total += (uint)command.Size;
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the file offset one past the end of the load command table.
    /// </summary>
    public uint LoadCommandsEndOffset => HeaderSize + SizeOfCommands;

    /// <summary>
    /// Gets the command locating this image's code signature, or null if it is unsigned.
    /// </summary>
    public MachOLinkEditDataCommand? CodeSignature
        => LoadCommands.OfType<MachOLinkEditDataCommand>().FirstOrDefault(c => c.Type == MachOLoadCommandType.CodeSignature);

    /// <summary>
    /// Gets whether this image has been changed since it was signed, which would leave the
    /// signature covering bytes that are no longer there.
    /// </summary>
    /// <remarks>
    /// Set by the editing operations and cleared by <see cref="AdHocSign"/>. Writing an image in
    /// this state fails, because the result would look signed and be refused at execution. Sign
    /// again after editing.
    /// <para>
    /// This follows the editing operations, not arbitrary writes to the model. Reaching into a
    /// load command or a piece of content directly still invalidates a signature without being
    /// noticed here.
    /// </para>
    /// </remarks>
    public bool IsCodeSignatureStale { get; internal set; }

    /// <summary>
    /// Gets the padding the linker left after the load command table, or null if there is none.
    /// </summary>
    /// <remarks>
    /// Linkers leave this gap so commands can be added later without moving anything. Adding a
    /// command consumes it from the front, which is how <c>install_name_tool</c> edits an image
    /// without relinking it.
    /// </remarks>
    public MachOContent? LoadCommandPadding { get; internal set; }

    /// <summary>
    /// Gets the file offset at which content after the load commands and their padding begins.
    /// </summary>
    public ulong ContentStartOffset
        => LoadCommandPadding is { } padding ? padding.Position + padding.Size : LoadCommandsEndOffset;

    /// <summary>
    /// Gets the number of bytes the load command table can still grow by without moving any
    /// content. A negative value means the current commands no longer fit.
    /// </summary>
    public long AvailableLoadCommandSpace => (long)ContentStartOffset - LoadCommandsEndOffset;

    /// <summary>
    /// Rewrites the file offsets that point at relocatable data: the tables in <c>__LINKEDIT</c>
    /// and the relocations of each section. This lets that data be moved without each caller
    /// knowing which commands record where it is.
    /// </summary>
    /// <param name="mapper">Maps an old file offset to its new one.</param>
    /// <remarks>
    /// Placement is deliberately not included. A segment's file offset and a section's file
    /// offset say where something is mapped, not merely where it is stored: a section's address
    /// is its segment's address plus its distance from the segment's file offset, so changing
    /// either moves the thing in memory and invalidates what refers to it. Moving a segment or a
    /// section is a different operation from relocating a blob nothing addresses, and this is
    /// only the second one.
    /// <para>
    /// No bytes are moved here; only the offsets recording where they are.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is null.</exception>
    public void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        foreach (var command in LoadCommands)
        {
            command.UpdateFileOffsets(mapper);
        }
    }

    /// <summary>
    /// Computes the size this image occupies when written, without writing it.
    /// </summary>
    /// <returns>The number of bytes <see cref="Write(System.IO.Stream)"/> produces.</returns>
    /// <remarks>
    /// Content sits at the offsets recorded for it, so the size is where the furthest of it
    /// ends. An image with no content at all is just its header, commands and their padding.
    /// </remarks>
    public ulong ComputeFileSize()
    {
        ulong end = 0;
        foreach (var content in Content)
        {
            end = Math.Max(end, content.Position + content.Size);
        }
        return end;
    }

    /// <summary>
    /// Finds a segment by name.
    /// </summary>
    /// <param name="name">The segment name, such as <c>__TEXT</c>.</param>
    /// <returns>The segment, or null if this image has no segment with that name.</returns>
    public MachOSegment? FindSegment(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var segment in Segments)
        {
            if (segment.Name == name) return segment;
        }
        return null;
    }

    /// <summary>
    /// Places the content of this image.
    /// </summary>
    /// <remarks>
    /// Only the header, the load command table and the padding between the table and what follows
    /// it are placed. Everything else keeps the position recorded for it, because a section's
    /// address is its segment's address plus its distance from the segment's file offset, so
    /// moving content in the file would move it in memory. The padding is what absorbs a command
    /// table that has grown, which is the whole of the room an edit has to work in.
    /// </remarks>
    protected override void UpdateLayoutCore(MachOVisitorContext context)
    {
        foreach (var content in Content)
        {
            content.UpdateLayout(context);
        }

        ulong cursor = 0;
        for (var i = 0; i < Content.Count; i++)
        {
            var content = Content[i];

            switch (content)
            {
                case MachOHeaderContent:
                case MachOLoadCommandTable:
                    content.Position = cursor;
                    break;

                case MachOLoadCommandPadding padding:
                    padding.Position = cursor;

                    // The padding runs from the end of the commands to whatever comes next, so a
                    // table that has grown eats into it and one that has shrunk gives back.
                    var next = i + 1 < Content.Count ? Content[i + 1].Position : cursor;
                    if (next < cursor)
                    {
                        context.Diagnostics.Error(
                            DiagnosticId.MACHO_ERR_NoRoomForLoadCommands,
                            $"The load commands now end at {cursor} but the content after them starts at {next}, so they no longer fit.");
                        return;
                    }
                    padding.Size = next - cursor;
                    break;
            }

            cursor = content.Position + content.Size;
        }
    }

    /// <inheritdoc />
    protected override void PrintName(StringBuilder builder) => builder.Append(nameof(MachOFile));

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{CpuType} {FileType}, {(Is64Bit ? "64-bit" : "32-bit")}, Commands = {LoadCommands.Count}");
        return true;
    }
}
