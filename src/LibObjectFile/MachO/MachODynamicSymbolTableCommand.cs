// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The dynamic symbol table load command (<c>LC_DYSYMTAB</c>).
/// </summary>
/// <remarks>
/// The index and count pairs describe runs within the table located by
/// <see cref="MachOSymbolTableCommand"/>, which the linker sorts into local, then external, then
/// undefined symbols. The remaining pairs locate tables of their own in <c>__LINKEDIT</c>. The
/// indirect symbol table is the one that matters at runtime: the stub and symbol pointer sections
/// index into it through their <c>reserved1</c> field.
/// </remarks>
public sealed class MachODynamicSymbolTableCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 80;

    /// <summary>The size of one table of contents entry (<c>dylib_table_of_contents</c>).</summary>
    public const uint TableOfContentsEntrySize = 8;

    /// <summary>The size of one external reference entry (<c>dylib_reference</c>).</summary>
    public const uint ExternalReferenceEntrySize = 4;

    /// <summary>The size of one indirect symbol entry, which is an index into the symbol table.</summary>
    public const uint IndirectSymbolEntrySize = 4;

    /// <summary>
    /// Gets the size of one module table entry for the given image width
    /// (<c>dylib_module</c> or <c>dylib_module_64</c>).
    /// </summary>
    /// <param name="is64Bit">Whether the containing image is 64-bit.</param>
    /// <returns>56 for a 64-bit image, 52 for a 32-bit one, the difference being one 64-bit field.</returns>
    public static uint GetModuleTableEntrySize(bool is64Bit) => is64Bit ? 56u : 52u;

    /// <summary>Gets or sets the index of the first local symbol.</summary>
    public uint LocalSymbolIndex { get; set; }

    /// <summary>Gets or sets the number of local symbols.</summary>
    public uint LocalSymbolCount { get; set; }

    /// <summary>Gets or sets the index of the first externally defined symbol.</summary>
    public uint ExternalSymbolIndex { get; set; }

    /// <summary>Gets or sets the number of externally defined symbols.</summary>
    public uint ExternalSymbolCount { get; set; }

    /// <summary>Gets or sets the index of the first undefined symbol.</summary>
    public uint UndefinedSymbolIndex { get; set; }

    /// <summary>Gets or sets the number of undefined symbols.</summary>
    public uint UndefinedSymbolCount { get; set; }

    /// <summary>Gets or sets the file offset of the table of contents, used only by static libraries.</summary>
    public uint TableOfContentsOffset { get; set; }

    /// <summary>Gets or sets the number of table of contents entries.</summary>
    public uint TableOfContentsCount { get; set; }

    /// <summary>Gets or sets the file offset of the module table.</summary>
    public uint ModuleTableOffset { get; set; }

    /// <summary>Gets or sets the number of module table entries.</summary>
    public uint ModuleTableCount { get; set; }

    /// <summary>Gets or sets the file offset of the external reference table.</summary>
    public uint ExternalReferenceOffset { get; set; }

    /// <summary>Gets or sets the number of external reference entries.</summary>
    public uint ExternalReferenceCount { get; set; }

    /// <summary>Gets or sets the file offset of the indirect symbol table.</summary>
    public uint IndirectSymbolOffset { get; set; }

    /// <summary>Gets or sets the number of indirect symbol entries, each a 4-byte symbol index.</summary>
    public uint IndirectSymbolCount { get; set; }

    /// <summary>Gets or sets the file offset of the external relocation entries.</summary>
    public uint ExternalRelocationOffset { get; set; }

    /// <summary>Gets or sets the number of external relocation entries.</summary>
    public uint ExternalRelocationCount { get; set; }

    /// <summary>Gets or sets the file offset of the local relocation entries.</summary>
    public uint LocalRelocationOffset { get; set; }

    /// <summary>Gets or sets the number of local relocation entries.</summary>
    public uint LocalRelocationCount { get; set; }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (TableOfContentsOffset != 0) TableOfContentsOffset = mapper(TableOfContentsOffset);
        if (ModuleTableOffset != 0) ModuleTableOffset = mapper(ModuleTableOffset);
        if (ExternalReferenceOffset != 0) ExternalReferenceOffset = mapper(ExternalReferenceOffset);
        if (IndirectSymbolOffset != 0) IndirectSymbolOffset = mapper(IndirectSymbolOffset);
        if (ExternalRelocationOffset != 0) ExternalRelocationOffset = mapper(ExternalRelocationOffset);
        if (LocalRelocationOffset != 0) LocalRelocationOffset = mapper(LocalRelocationOffset);
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawDysymtabCommand), out RawDysymtabCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_DYSYMTAB at 0x{Position:X}");
            return;
        }

        LocalSymbolIndex = raw.LocalSymbolIndex;
        LocalSymbolCount = raw.LocalSymbolCount;
        ExternalSymbolIndex = raw.ExternalSymbolIndex;
        ExternalSymbolCount = raw.ExternalSymbolCount;
        UndefinedSymbolIndex = raw.UndefinedSymbolIndex;
        UndefinedSymbolCount = raw.UndefinedSymbolCount;
        TableOfContentsOffset = raw.TableOfContentsOffset;
        TableOfContentsCount = raw.TableOfContentsCount;
        ModuleTableOffset = raw.ModuleTableOffset;
        ModuleTableCount = raw.ModuleTableCount;
        ExternalReferenceOffset = raw.ExternalReferenceOffset;
        ExternalReferenceCount = raw.ExternalReferenceCount;
        IndirectSymbolOffset = raw.IndirectSymbolOffset;
        IndirectSymbolCount = raw.IndirectSymbolCount;
        ExternalRelocationOffset = raw.ExternalRelocationOffset;
        ExternalRelocationCount = raw.ExternalRelocationCount;
        LocalRelocationOffset = raw.LocalRelocationOffset;
        LocalRelocationCount = raw.LocalRelocationCount;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.Write(new RawDysymtabCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            LocalSymbolIndex = LocalSymbolIndex,
            LocalSymbolCount = LocalSymbolCount,
            ExternalSymbolIndex = ExternalSymbolIndex,
            ExternalSymbolCount = ExternalSymbolCount,
            UndefinedSymbolIndex = UndefinedSymbolIndex,
            UndefinedSymbolCount = UndefinedSymbolCount,
            TableOfContentsOffset = TableOfContentsOffset,
            TableOfContentsCount = TableOfContentsCount,
            ModuleTableOffset = ModuleTableOffset,
            ModuleTableCount = ModuleTableCount,
            ExternalReferenceOffset = ExternalReferenceOffset,
            ExternalReferenceCount = ExternalReferenceCount,
            IndirectSymbolOffset = IndirectSymbolOffset,
            IndirectSymbolCount = IndirectSymbolCount,
            ExternalRelocationOffset = ExternalRelocationOffset,
            ExternalRelocationCount = ExternalRelocationCount,
            LocalRelocationOffset = LocalRelocationOffset,
            LocalRelocationCount = LocalRelocationCount,
        });
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Local = {LocalSymbolCount}, External = {ExternalSymbolCount}, Undefined = {UndefinedSymbolCount}, Indirect = {IndirectSymbolCount}");
        return true;
    }
}
