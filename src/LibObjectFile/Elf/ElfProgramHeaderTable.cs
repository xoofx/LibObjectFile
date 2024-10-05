// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// The program header table.
/// </summary>
public sealed partial class ElfProgramHeaderTable : ElfContentData
{
    private bool _is32;

    public ElfProgramHeaderTable()
    {
    }

    public uint AdditionalEntrySize { get; set; }

    public override void Read(ElfReader reader)
    {
        if (_is32)
        {
            Read32(reader);
        }
        else
        {
            Read64(reader);
        }
    }

    public override void Write(ElfWriter writer)
    {
        if (_is32)
        {
            Write32(writer);
        }
        else
        {
            Write64(writer);
        }
    }


    public override void Verify(ElfVisitorContext context)
    {
        var segments = Parent!.Segments;
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment.AdditionalData.Length != AdditionalEntrySize)
            {
                context.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidProgramHeaderAdditionalDataSize, $"Invalid additional data size [{segment.AdditionalData.Length}] for program header #{i}. Expecting [{AdditionalEntrySize}]");
            }
        }
    }

    protected override unsafe void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = (ulong)(Parent!.Segments.Count * (AdditionalEntrySize + (_is32 ? sizeof(ElfNative.Elf32_Phdr) : sizeof(ElfNative.Elf64_Phdr))));
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        base.ValidateParent(parent);
        var elf = (ElfFile)parent;
        _is32 = elf.FileClass == ElfFileClass.Is32;
        Alignment = _is32 ? 4u : 8u;
    }
}
