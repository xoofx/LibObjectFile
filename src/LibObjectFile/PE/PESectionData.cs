// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for data contained in a <see cref="PESection"/>.
/// </summary>
public abstract class PESectionData : PEObject
{
    /// <summary>
    /// Gets the parent <see cref="PESection"/> of this section data.
    /// </summary>
    public PESection? Section
    {
        get => (PESection?)Parent;

        internal set => Parent = value;
    }

    protected abstract void Write(PEImageWriter writer);
}

/// <summary>
/// Defines a raw section data in a Portable Executable (PE) image.
/// </summary>
public sealed class PESectionMemoryData : PESectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionMemoryData"/> class.
    /// </summary>
    public PESectionMemoryData() : this(Array.Empty<byte>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionMemoryData"/> class.
    /// </summary>
    /// <param name="data">The raw data.</param>
    public PESectionMemoryData(Memory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the raw data.
    /// </summary>
    public Memory<byte> Data { get; set; }

    /// <inheritdoc />
    public override ulong Size
    {
        get => (ulong)Data.Length;
        set => throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
    }

    protected override void Write(PEImageWriter writer) => writer.Write(Data.Span);
}

/// <summary>
/// Gets a stream section data in a Portable Executable (PE) image.
/// </summary>
public sealed class PESectionStreamData : PESectionData
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

    protected override void Write(PEImageWriter writer)
    {
        Stream.Position = 0;
        Stream.CopyTo(writer.Stream);
    }
}
