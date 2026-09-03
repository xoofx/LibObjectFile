// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

/// <summary>
/// Reads a <see cref="MachOFile"/> from a <see cref="Stream"/>.
/// </summary>
public sealed class MachOReader : ObjectFileReaderWriter
{
    internal MachOReader(MachOFile file, Stream stream, MachOReaderOptions options) : base(file, stream)
    {
        Options = options;
        VisitorContext = new MachOVisitorContext(file, Diagnostics);
    }

    internal MachOReader(MachOFile file, Stream stream, MachOReaderOptions options, DiagnosticBag diagnostics) : base(file, stream, diagnostics)
    {
        Options = options;
        VisitorContext = new MachOVisitorContext(file, Diagnostics);
    }

    /// <summary>
    /// Gets the file being read.
    /// </summary>
    public new MachOFile File => (MachOFile)base.File;

    /// <summary>
    /// Gets the context carrying the diagnostics collected while reading.
    /// </summary>
    public MachOVisitorContext VisitorContext { get; }

    /// <summary>
    /// Gets the options used for reading.
    /// </summary>
    public MachOReaderOptions Options { get; }

    /// <inheritdoc />
    public override bool KeepOriginalStreamForSubStreams => Options.UseSubStream;

    /// <summary>
    /// Converts a reader to the visitor context it carries.
    /// </summary>
    /// <param name="reader">The reader.</param>
    public static implicit operator MachOVisitorContext(MachOReader reader) => reader.VisitorContext;
}
