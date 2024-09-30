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
    private uint _requiredPositionAlignment;
    private uint _requiredSizeAlignment;

    internal static PEStreamSectionData Empty = new(System.IO.Stream.Null);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamSectionData"/> class.
    /// </summary>
    public PEStreamSectionData() : this(new MemoryStream())
    {
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
        _requiredPositionAlignment = 1;
        _requiredSizeAlignment = 1;
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

    /// <summary>
    /// Gets or sets the preferred position alignment for this section data.
    /// </summary>
    public uint RequiredPositionAlignment
    {
        get => _requiredPositionAlignment;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1U);
            _requiredPositionAlignment = value;
        }
    }

    /// <summary>
    /// Gets or sets the preferred size alignment for this section data.
    /// </summary>
    public uint RequiredSizeAlignment
    {
        get => _requiredSizeAlignment;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1U);
            _requiredSizeAlignment = value;
        }
    }
    
    protected override void UpdateLayoutCore(PELayoutContext layoutContext)
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

    public override uint GetRequiredPositionAlignment(PEFile file) => _requiredPositionAlignment;

    public override uint GetRequiredSizeAlignment(PEFile file) => _requiredSizeAlignment;
}