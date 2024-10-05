// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// The program header table.
/// </summary>
public abstract class ElfProgramHeaderTable : ElfContentData
{
    protected ElfProgramHeaderTable()
    {
    }

    public uint AdditionalEntrySize { get; set; }

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
}
