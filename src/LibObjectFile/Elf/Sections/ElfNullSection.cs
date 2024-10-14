// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// A null section with the type <see cref="ElfSectionType.Null"/>.
/// </summary>
public sealed class ElfNullSection() : ElfSection(ElfSectionType.Null)
{
    public override void Verify(ElfVisitorContext context)
    {
        if (Type != ElfSectionType.Null ||
            Flags != ElfSectionFlags.None ||
            !Name.IsEmpty ||
            VirtualAddress != 0 ||
            VirtualAddressAlignment != 0 ||
            !Link.IsEmpty ||
            !Info.IsEmpty ||
            Position != 0 ||
            Size != 0)
        {
            context.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidNullSection, "Invalid Null section. This section should not be modified and all properties must be null");
        }
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
    }
}