// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;

namespace LibObjectFile.MachO;

/// <summary>
/// Extension methods for <see cref="MachOFile"/> to print their layout in text form, similar to
/// <c>otool -h -l</c>.
/// </summary>
/// <remarks>
/// The field names are the ones the format uses rather than the ones this library exposes, so
/// that output can be read next to <c>otool</c>'s without translating between them.
/// </remarks>
public static class MachOPrinter
{
    /// <summary>
    /// Prints a <see cref="MachOFile"/> to the specified writer.
    /// </summary>
    /// <param name="file">The image to print.</param>
    /// <param name="writer">The destination text writer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="writer"/> is null.</exception>
    public static void Print(this MachOFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        PrintHeader(file, writer);
        PrintLoadCommands(file, writer);
    }

    /// <summary>
    /// Prints the Mach-O header.
    /// </summary>
    /// <param name="file">The image to print.</param>
    /// <param name="writer">The destination text writer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="writer"/> is null.</exception>
    public static void PrintHeader(MachOFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Mach header:");
        Field(writer, "magic", file.Is64Bit ? "MH_MAGIC_64" : "MH_MAGIC");
        Field(writer, "cputype", file.CpuType.ToString());
        Field(writer, "cpusubtype", $"0x{file.CpuSubType:x8}");
        Field(writer, "filetype", file.FileType.ToString());
        Field(writer, "ncmds", file.LoadCommands.Count.ToString());
        Field(writer, "sizeofcmds", file.SizeOfCommands.ToString());
        Field(writer, "flags", file.Flags == MachOHeaderFlags.None ? "0x0" : $"0x{(uint)file.Flags:x} {file.Flags}");
        writer.WriteLine();
    }

    /// <summary>
    /// Prints every load command, in the order the image stores them.
    /// </summary>
    /// <param name="file">The image to print.</param>
    /// <param name="writer">The destination text writer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="writer"/> is null.</exception>
    public static void PrintLoadCommands(MachOFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        for (var i = 0; i < file.LoadCommands.Count; i++)
        {
            var command = file.LoadCommands[i];
            writer.WriteLine($"Load command {i}");
            Field(writer, "cmd", DescribeType(command.Type));
            Field(writer, "cmdsize", command.Size.ToString());
            PrintCommandBody(command, writer);
        }
    }

    private static void PrintCommandBody(MachOLoadCommand command, TextWriter writer)
    {
        switch (command)
        {
            case MachOSegment segment:
                PrintSegment(segment, writer);
                break;
            case MachODylibCommand dylib:
                Field(writer, "name", dylib.Name);
                Field(writer, "timestamp", dylib.Timestamp.ToString());
                Field(writer, "current version", MachOVersion.Decode(dylib.CurrentVersion).ToString());
                Field(writer, "compatibility version", MachOVersion.Decode(dylib.CompatibilityVersion).ToString());
                break;
            case MachOPathCommand path:
                Field(writer, "path", path.Path);
                break;
            case MachOSymbolTableCommand symtab:
                Field(writer, "symoff", symtab.SymbolOffset.ToString());
                Field(writer, "nsyms", symtab.SymbolCount.ToString());
                Field(writer, "stroff", symtab.StringOffset.ToString());
                Field(writer, "strsize", symtab.StringSize.ToString());
                break;
            case MachODynamicSymbolTableCommand dysymtab:
                Field(writer, "nlocalsym", dysymtab.LocalSymbolCount.ToString());
                Field(writer, "nextdefsym", dysymtab.ExternalSymbolCount.ToString());
                Field(writer, "nundefsym", dysymtab.UndefinedSymbolCount.ToString());
                Field(writer, "indirectsymoff", dysymtab.IndirectSymbolOffset.ToString());
                Field(writer, "nindirectsyms", dysymtab.IndirectSymbolCount.ToString());
                break;
            case MachODyldInfoCommand dyldInfo:
                Field(writer, "rebase_off", dyldInfo.RebaseOffset.ToString());
                Field(writer, "rebase_size", dyldInfo.RebaseSize.ToString());
                Field(writer, "bind_off", dyldInfo.BindOffset.ToString());
                Field(writer, "bind_size", dyldInfo.BindSize.ToString());
                Field(writer, "lazy_bind_off", dyldInfo.LazyBindOffset.ToString());
                Field(writer, "lazy_bind_size", dyldInfo.LazyBindSize.ToString());
                Field(writer, "export_off", dyldInfo.ExportOffset.ToString());
                Field(writer, "export_size", dyldInfo.ExportSize.ToString());
                break;
            case MachOLinkEditDataCommand linkEdit:
                Field(writer, "dataoff", linkEdit.DataOffset.ToString());
                Field(writer, "datasize", linkEdit.DataSize.ToString());
                break;
            case MachOMainCommand main:
                Field(writer, "entryoff", main.EntryOffset.ToString());
                Field(writer, "stacksize", main.StackSize.ToString());
                break;
            case MachOUuidCommand uuid:
                Field(writer, "uuid", uuid.Uuid.ToString("D").ToUpperInvariant());
                break;
            case MachOVersionMinCommand versionMin:
                Field(writer, "version", versionMin.MinOS.ToString());
                Field(writer, "sdk", versionMin.Sdk.ToString());
                break;
            case MachOBuildVersionCommand build:
                Field(writer, "platform", build.Platform.ToString());
                Field(writer, "minos", build.MinOS.ToString());
                Field(writer, "sdk", build.Sdk.ToString());
                Field(writer, "ntools", build.Tools.Count.ToString());
                foreach (var tool in build.Tools)
                {
                    Field(writer, "tool", tool.Tool.ToString());
                    Field(writer, "version", tool.Version.ToString());
                }
                break;
            case MachOSourceVersionCommand source:
                Field(writer, "version", source.Version.ToString());
                break;
            case MachOTwoLevelHintsCommand hints:
                Field(writer, "offset", hints.Offset.ToString());
                Field(writer, "nhints", hints.HintCount.ToString());
                break;
            case MachOThreadCommand thread:
                foreach (var state in thread.States)
                {
                    Field(writer, "flavor", state.Flavor.ToString());
                    Field(writer, "count", state.Registers.Length.ToString());
                }
                break;
            case MachOUnknownLoadCommand unknown:
                Field(writer, "payload", $"{unknown.Payload.Length} bytes");
                break;
        }
    }

    private static void PrintSegment(MachOSegment segment, TextWriter writer)
    {
        Field(writer, "segname", segment.Name);
        Field(writer, "vmaddr", $"0x{segment.VmAddress:x8}");
        Field(writer, "vmsize", $"0x{segment.VmSize:x8}");
        Field(writer, "fileoff", segment.FileOffset.ToString());
        Field(writer, "filesize", segment.FileSize.ToString());
        Field(writer, "maxprot", DescribeProtection(segment.MaxProtection));
        Field(writer, "initprot", DescribeProtection(segment.InitProtection));
        Field(writer, "nsects", segment.Sections.Count.ToString());
        Field(writer, "flags", $"0x{(uint)segment.SegmentFlags:x}");

        foreach (var section in segment.Sections)
        {
            writer.WriteLine("Section");
            Field(writer, "sectname", section.Name);
            Field(writer, "segname", section.SegmentName);
            Field(writer, "addr", $"0x{section.Address:x8}");
            Field(writer, "size", $"0x{section.Size:x8}");
            Field(writer, "offset", section.FileOffset.ToString());
            Field(writer, "align", $"2^{section.Align} ({1 << (int)section.Align})");
            Field(writer, "reloff", section.RelocationOffset.ToString());
            Field(writer, "nreloc", section.NumberOfRelocations.ToString());
            Field(writer, "type", section.SectionType.ToString());
            Field(writer, "attributes", section.Attributes == MachOSectionAttributes.None ? "(none)" : section.Attributes.ToString());
            Field(writer, "reserved1", section.Reserved1.ToString());
            Field(writer, "reserved2", section.Reserved2.ToString());
        }
    }

    /// <summary>
    /// Names a command by its <c>LC_</c> spelling rather than the enumeration's, so output can be
    /// compared with <c>otool</c> directly. The mapping is written out rather than derived from
    /// the enumeration names, because several do not follow from them: <c>LC_UNIXTHREAD</c> is
    /// one word and <c>LC_VERSION_MIN_MACOSX</c> breaks in places the casing does not.
    /// An unmodelled command keeps its raw value, so the output stays useful against an image
    /// built by a newer linker.
    /// </summary>
    private static string DescribeType(MachOLoadCommandType type) => type switch
    {
        MachOLoadCommandType.Segment => "LC_SEGMENT",
        MachOLoadCommandType.SymbolTable => "LC_SYMTAB",
        MachOLoadCommandType.SymbolSegment => "LC_SYMSEG",
        MachOLoadCommandType.Thread => "LC_THREAD",
        MachOLoadCommandType.UnixThread => "LC_UNIXTHREAD",
        MachOLoadCommandType.LoadFixedVMLibrary => "LC_LOADFVMLIB",
        MachOLoadCommandType.IdFixedVMLibrary => "LC_IDFVMLIB",
        MachOLoadCommandType.Identification => "LC_IDENT",
        MachOLoadCommandType.FixedVMFile => "LC_FVMFILE",
        MachOLoadCommandType.Prepage => "LC_PREPAGE",
        MachOLoadCommandType.DynamicSymbolTable => "LC_DYSYMTAB",
        MachOLoadCommandType.LoadDylib => "LC_LOAD_DYLIB",
        MachOLoadCommandType.IdDylib => "LC_ID_DYLIB",
        MachOLoadCommandType.LoadDylinker => "LC_LOAD_DYLINKER",
        MachOLoadCommandType.IdDylinker => "LC_ID_DYLINKER",
        MachOLoadCommandType.PreboundDylib => "LC_PREBOUND_DYLIB",
        MachOLoadCommandType.Routines => "LC_ROUTINES",
        MachOLoadCommandType.SubFramework => "LC_SUB_FRAMEWORK",
        MachOLoadCommandType.SubUmbrella => "LC_SUB_UMBRELLA",
        MachOLoadCommandType.SubClient => "LC_SUB_CLIENT",
        MachOLoadCommandType.SubLibrary => "LC_SUB_LIBRARY",
        MachOLoadCommandType.TwoLevelHints => "LC_TWOLEVEL_HINTS",
        MachOLoadCommandType.PrebindChecksum => "LC_PREBIND_CKSUM",
        MachOLoadCommandType.LoadWeakDylib => "LC_LOAD_WEAK_DYLIB",
        MachOLoadCommandType.Segment64 => "LC_SEGMENT_64",
        MachOLoadCommandType.Routines64 => "LC_ROUTINES_64",
        MachOLoadCommandType.Uuid => "LC_UUID",
        MachOLoadCommandType.RPath => "LC_RPATH",
        MachOLoadCommandType.CodeSignature => "LC_CODE_SIGNATURE",
        MachOLoadCommandType.SegmentSplitInfo => "LC_SEGMENT_SPLIT_INFO",
        MachOLoadCommandType.ReexportDylib => "LC_REEXPORT_DYLIB",
        MachOLoadCommandType.LazyLoadDylib => "LC_LAZY_LOAD_DYLIB",
        MachOLoadCommandType.EncryptionInfo => "LC_ENCRYPTION_INFO",
        MachOLoadCommandType.DyldInfo => "LC_DYLD_INFO",
        MachOLoadCommandType.DyldInfoOnly => "LC_DYLD_INFO_ONLY",
        MachOLoadCommandType.LoadUpwardDylib => "LC_LOAD_UPWARD_DYLIB",
        MachOLoadCommandType.VersionMinMacOSX => "LC_VERSION_MIN_MACOSX",
        MachOLoadCommandType.VersionMinIPhoneOS => "LC_VERSION_MIN_IPHONEOS",
        MachOLoadCommandType.FunctionStarts => "LC_FUNCTION_STARTS",
        MachOLoadCommandType.DyldEnvironment => "LC_DYLD_ENVIRONMENT",
        MachOLoadCommandType.Main => "LC_MAIN",
        MachOLoadCommandType.DataInCode => "LC_DATA_IN_CODE",
        MachOLoadCommandType.SourceVersion => "LC_SOURCE_VERSION",
        MachOLoadCommandType.DylibCodeSignDrs => "LC_DYLIB_CODE_SIGN_DRS",
        MachOLoadCommandType.EncryptionInfo64 => "LC_ENCRYPTION_INFO_64",
        MachOLoadCommandType.LinkerOption => "LC_LINKER_OPTION",
        MachOLoadCommandType.LinkerOptimizationHint => "LC_LINKER_OPTIMIZATION_HINT",
        MachOLoadCommandType.VersionMinTvOS => "LC_VERSION_MIN_TVOS",
        MachOLoadCommandType.VersionMinWatchOS => "LC_VERSION_MIN_WATCHOS",
        MachOLoadCommandType.Note => "LC_NOTE",
        MachOLoadCommandType.BuildVersion => "LC_BUILD_VERSION",
        MachOLoadCommandType.DyldExportsTrie => "LC_DYLD_EXPORTS_TRIE",
        MachOLoadCommandType.DyldChainedFixups => "LC_DYLD_CHAINED_FIXUPS",
        MachOLoadCommandType.FilesetEntry => "LC_FILESET_ENTRY",
        MachOLoadCommandType.AtomInfo => "LC_ATOM_INFO",
        _ => $"0x{(uint)type:x8}",
    };

    private static string DescribeProtection(MachOVmProtection protection)
    {
        if (protection == MachOVmProtection.None) return "---";

        Span<char> result = ['-', '-', '-'];
        if ((protection & MachOVmProtection.Read) != 0) result[0] = 'r';
        if ((protection & MachOVmProtection.Write) != 0) result[1] = 'w';
        if ((protection & MachOVmProtection.Execute) != 0) result[2] = 'x';
        return new string(result);
    }

    private static void Field(TextWriter writer, string name, string value)
        => writer.WriteLine($"{name,22} {value}");
}
