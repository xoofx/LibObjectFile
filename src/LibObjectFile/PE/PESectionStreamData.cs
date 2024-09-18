// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Gets a stream section data in a Portable Executable (PE) image.
/// </summary>
public class PESectionStreamData : PESectionData
{
    private Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionStreamData"/> class.
    /// </summary>
    public PESectionStreamData()
    {
        _stream = Stream.Null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionStreamData"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the data of this section data.</param>
    public PESectionStreamData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
    }

    /// <summary>
    /// Gets the stream containing the data of this section data.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set => _stream = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override ulong Size
    {
        get => (ulong)Stream.Length;
        set => throw new InvalidOperationException();
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
    }

    protected override void Read(PEImageReader reader)
    {
        // No need to read, as the data is already provided via a stream
    }

    protected override void Write(PEImageWriter writer)
    {
        Stream.Position = 0;
        Stream.CopyTo(writer.Stream);
    }
}