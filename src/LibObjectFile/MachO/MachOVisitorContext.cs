// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

/// <summary>
/// Context used when laying out or verifying a <see cref="MachOFile"/>.
/// </summary>
public sealed class MachOVisitorContext : VisitorContextBase<MachOFile>
{
    internal MachOVisitorContext(MachOFile file, DiagnosticBag diagnostics) : base(file, diagnostics)
    {
    }
}
