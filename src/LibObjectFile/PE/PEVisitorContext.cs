// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEVisitorContext : VisitorContextBase<PEFile>
{
    internal PEVisitorContext(PEFile peFile, DiagnosticBag diagnostics) : base(peFile, diagnostics)
    {
    }
}