// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

/// <summary>
/// Writes a <see cref="MachOFile"/> to a <see cref="Stream"/>.
/// </summary>
public sealed class MachOWriter : ObjectFileReaderWriter
{
    internal MachOWriter(MachOFile file, Stream stream, DiagnosticBag diagnostics) : base(file, stream, diagnostics)
    {
        VisitorContext = new MachOVisitorContext(file, Diagnostics);
    }

    /// <summary>
    /// Gets the file being written.
    /// </summary>
    public new MachOFile File => (MachOFile)base.File;

    /// <summary>
    /// Gets the context carrying the diagnostics collected while writing.
    /// </summary>
    public MachOVisitorContext VisitorContext { get; }

    /// <inheritdoc />
    public override bool KeepOriginalStreamForSubStreams => false;

    /// <summary>
    /// Converts a writer to the visitor context it carries.
    /// </summary>
    /// <param name="writer">The writer.</param>
    public static implicit operator MachOVisitorContext(MachOWriter writer) => writer.VisitorContext;
}
