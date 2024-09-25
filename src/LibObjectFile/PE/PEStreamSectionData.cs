// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Gets a stream section data in a Portable Executable (PE) image.
/// </summary>
public class PEStreamSectionData : PESectionData
{
    private Stream _stream;

    internal static PEStreamSectionData Empty = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamSectionData"/> class.
    /// </summary>
    public PEStreamSectionData()
    {
        _stream = Stream.Null;
        Size = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamSectionData"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the data of this section data.</param>
    public PEStreamSectionData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        Size = (ulong)stream.Length;
    }

    public override bool HasChildren => false;

    /// <summary>
    /// Gets the stream containing the data of this section data.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream = value;
            Size = (ulong)value.Length;
        }
    }

    public override void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = (ulong)Stream.Length;
    }

    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        Stream = reader.ReadAsStream(Size);
    }

    public override void Write(PEImageWriter writer)
    {
        Stream.Position = 0;
        Stream.CopyTo(writer.Stream);
    }

    public override int ReadAt(uint offset, Span<byte> destination)
    {
        Stream.Position = offset;
        return Stream.Read(destination);
    }

    public override void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        Stream.Position = offset;
        Stream.Write(source);
    }
}