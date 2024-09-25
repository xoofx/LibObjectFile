// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a stream extra data in the PE file <see cref="PEFile.ExtraDataAfterSections"/>.
/// </summary>
public class PEStreamExtraData : PEExtraData
{
    private Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamExtraData"/> class.
    /// </summary>
    public PEStreamExtraData()
    {
        _stream = Stream.Null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamExtraData"/> class.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    public PEStreamExtraData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        Size = (uint)stream.Length;
    }

    /// <summary>
    /// Gets or sets the data stream.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream = value;
            Size = (uint)value.Length;
        }
    }

    public override void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = (uint)Stream.Length;
    }

    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        Stream = reader.ReadAsStream(Size);
    }

    public override void Write(PEImageWriter writer)
    {
        Stream.CopyTo(writer.Stream);
    }
}