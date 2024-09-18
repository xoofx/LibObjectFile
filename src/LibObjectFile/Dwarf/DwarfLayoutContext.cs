// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.Dwarf;



public abstract class DwarfVisitorContext : VisitorContextBase<DwarfFile>
{
    internal DwarfVisitorContext(DwarfFile file, DiagnosticBag diagnostics) : base(file, diagnostics)
    {
    }
}


public sealed class DwarfLayoutContext : DwarfVisitorContext
{
    internal DwarfLayoutContext(DwarfFile file, DwarfLayoutConfig config, DiagnosticBag diagnostics) : base(file, diagnostics)
    {
        Config = config;
    }

    public DwarfLayoutConfig Config { get; }
       
    public DwarfUnit? CurrentUnit { get; internal set; }
}

public sealed class DwarfVerifyContext : DwarfVisitorContext
{
    internal DwarfVerifyContext(DwarfFile file, DiagnosticBag diagnostics) : base(file, diagnostics)
    {
    }
}