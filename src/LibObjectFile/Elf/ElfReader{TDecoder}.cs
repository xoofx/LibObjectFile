// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.IO;
using static System.Collections.Specialized.BitVector32;

namespace LibObjectFile.Elf;

/// <summary>
/// Internal implementation of <see cref="ElfReader"/> to read from a stream to an <see cref="ElfFile"/> instance.
/// </summary>
/// <typeparam name="TDecoder">The decoder used for LSB/MSB conversion</typeparam>
internal abstract class ElfReader<TDecoder> : ElfReader where TDecoder : struct, IElfDecoder
{
    private TDecoder _decoder;

    protected ElfReader(ElfFile file, Stream stream, ElfReaderOptions options) : base(file, stream, options)
    {
        _decoder = new TDecoder();
    }

    public override ElfSectionLink ResolveLink(ElfSectionLink link, string errorMessageFormat)
    {
        if (errorMessageFormat == null) throw new ArgumentNullException(nameof(errorMessageFormat));

        // Connect section Link instance
        if (!link.IsEmpty)
        {
            var _sectionStringTableIndex = (int)File.SectionHeaderStringTable?.Index ?? 0;

            if (link.SpecialIndex == _sectionStringTableIndex)
            {
                link = new ElfSectionLink(SectionHeaderStringTable);
            }
            else
            {
                var sectionIndex = link.SpecialIndex;

                bool sectionFound = false;
                if (sectionIndex < Sections.Count && Sections[(int)sectionIndex].SectionIndex == sectionIndex)
                {
                    link = new ElfSectionLink(Sections[(int)sectionIndex]);
                    sectionFound = true;
                }
                else
                {
                    foreach (var section in Sections)
                    {
                        if (section.SectionIndex == sectionIndex)
                        {
                            link = new ElfSectionLink(section);
                            sectionFound = true;
                            break;
                        }
                    }
                }

                if (!sectionFound)
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidResolvedLink, string.Format(errorMessageFormat, link.SpecialIndex));
                }
            }
        }

        return link;
    }

    public override ushort Decode(ElfNative.Elf32_Half src) => _decoder.Decode(src);

    public override ushort Decode(ElfNative.Elf64_Half src) => _decoder.Decode(src);

    public override uint Decode(ElfNative.Elf32_Word src) => _decoder.Decode(src);

    public override uint Decode(ElfNative.Elf64_Word src) => _decoder.Decode(src);

    public override int Decode(ElfNative.Elf32_Sword src) => _decoder.Decode(src);

    public override int Decode(ElfNative.Elf64_Sword src) => _decoder.Decode(src);

    public override ulong Decode(ElfNative.Elf32_Xword src) => _decoder.Decode(src);

    public override long Decode(ElfNative.Elf32_Sxword src) => _decoder.Decode(src);

    public override ulong Decode(ElfNative.Elf64_Xword src) => _decoder.Decode(src);

    public override long Decode(ElfNative.Elf64_Sxword src) => _decoder.Decode(src);

    public override uint Decode(ElfNative.Elf32_Addr src) => _decoder.Decode(src);

    public override ulong Decode(ElfNative.Elf64_Addr src) => _decoder.Decode(src);

    public override uint Decode(ElfNative.Elf32_Off src) => _decoder.Decode(src);

    public override ulong Decode(ElfNative.Elf64_Off src) => _decoder.Decode(src);

    public override ushort Decode(ElfNative.Elf32_Section src) => _decoder.Decode(src);

    public override ushort Decode(ElfNative.Elf64_Section src) => _decoder.Decode(src);

    public override ushort Decode(ElfNative.Elf32_Versym src) => _decoder.Decode(src);

    public override ushort Decode(ElfNative.Elf64_Versym src) => _decoder.Decode(src);
}