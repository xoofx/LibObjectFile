// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A Portable Executable file that can be read, modified and written.
/// </summary>
partial class PEFile
{
    /// <summary>
    /// Verifies the validity of this PE file.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to output errors.</param>
    public void Verify(DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var context = new PEVerifyContext(this, diagnostics);
        Verify(context);
    }

    /// <inheritdoc />
    public override void Verify(PEVerifyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!TryVerifyAlignment(context.Diagnostics))
        {
            return;
        }

        foreach (var data in ExtraDataBeforeSections)
        {
            data.Verify(context);
        }
        
        foreach (var section in Sections)
        {
            section.Verify(context);
        }

        foreach (var data in ExtraDataAfterSections)
        {
            data.Verify(context);
        }
    }
}
