// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// Base class for reading and building an <see cref="ElfFile"/> from a <see cref="Stream"/>.
/// </summary>
public abstract class ElfReader : ObjectFileReaderWriter, IElfDecoder
{
    private protected ElfReader(ElfFile file, Stream stream, ElfReaderOptions readerOptions) : base(file, stream)
    {
        Options = readerOptions;
        VisitorContext = new ElfVisitorContext(file, Diagnostics);
    }

    public new ElfFile File => (ElfFile)base.File;

    public ElfVisitorContext VisitorContext { get; }

    /// <summary>
    /// Gets the <see cref="ElfReaderOptions"/> used for reading the <see cref="ElfFile"/>
    /// </summary>
    public ElfReaderOptions Options { get; }

    public override bool KeepOriginalStreamForSubStreams => Options.ReadOnly;

    public ElfSectionLink ResolveLink(ElfSectionLink link, string errorMessageFormat)
    {
        ArgumentNullException.ThrowIfNull(errorMessageFormat);

        // Connect section Link instance
        if (!link.IsEmpty)
        {
            if (link.SpecialIndex >= File.Sections.Count)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidResolvedLink, string.Format(errorMessageFormat, link.SpecialIndex));
            }
            else
            {
                link = new ElfSectionLink(File.Sections[link.SpecialIndex]);
            }
        }

        return link;
    }

    internal static ElfReader Create(ElfFile file, Stream stream, ElfReaderOptions options)
    {
        var thisComputerEncoding = BitConverter.IsLittleEndian ? ElfEncoding.Lsb : ElfEncoding.Msb;
        return file.Encoding == thisComputerEncoding ? (ElfReader) new ElfReaderDirect(file, stream, options) : new ElfReaderSwap(file, stream, options);
    }

    public abstract ushort Decode(ElfNative.Elf32_Half src);
    public abstract ushort Decode(ElfNative.Elf64_Half src);
    public abstract uint Decode(ElfNative.Elf32_Word src);
    public abstract uint Decode(ElfNative.Elf64_Word src);
    public abstract int Decode(ElfNative.Elf32_Sword src);
    public abstract int Decode(ElfNative.Elf64_Sword src);
    public abstract ulong Decode(ElfNative.Elf32_Xword src);
    public abstract long Decode(ElfNative.Elf32_Sxword src);
    public abstract ulong Decode(ElfNative.Elf64_Xword src);
    public abstract long Decode(ElfNative.Elf64_Sxword src);
    public abstract uint Decode(ElfNative.Elf32_Addr src);
    public abstract ulong Decode(ElfNative.Elf64_Addr src);
    public abstract uint Decode(ElfNative.Elf32_Off src);
    public abstract ulong Decode(ElfNative.Elf64_Off src);
    public abstract ushort Decode(ElfNative.Elf32_Section src);
    public abstract ushort Decode(ElfNative.Elf64_Section src);
    public abstract ushort Decode(ElfNative.Elf32_Versym src);
    public abstract ushort Decode(ElfNative.Elf64_Versym src);

    public static implicit operator ElfVisitorContext(ElfReader reader) => reader.VisitorContext;
}