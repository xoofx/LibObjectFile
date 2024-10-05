// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf;

public sealed class ElfNoBitsSection : ElfSection
{
    public ElfNoBitsSection() : base(ElfSectionType.NoBits)
    {
    }

    public override void Read(ElfReader reader)
    {
    }

    public override void Write(ElfWriter writer)
    {
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
    }
}