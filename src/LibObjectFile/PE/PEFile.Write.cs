// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

partial class PEFile
{
    /// <summary>
    /// Writes this PE file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void Write(Stream stream)
    {
        if (!TryWrite(stream, out var diagnostics))
        {
            throw new ObjectFileException($"Invalid PE File", diagnostics);
        }
    }

    /// <summary>
    /// Tries to write this PE file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="diagnostics">The output diagnostics</param>
    /// <returns><c>true</c> if writing was successful. otherwise <c>false</c></returns>
    public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        
        var peWriter = new PEImageWriter(this, stream);
        diagnostics = peWriter.Diagnostics;

        Verify(diagnostics);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        UpdateLayout(diagnostics);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        Write(peWriter);
        
        return !diagnostics.HasErrors;
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}