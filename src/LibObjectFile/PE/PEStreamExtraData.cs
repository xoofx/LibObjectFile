// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a stream extra data in the PE file <see cref="PEFile.ExtraDataAfterSections"/>.
/// </summary>
public sealed class PEStreamExtraData : PEExtraData
{
    private Stream _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamExtraData"/> class.
    /// </summary>
    public PEStreamExtraData()
    {
        _data = Stream.Null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEStreamExtraData"/> class.
    /// </summary>
    /// <param name="data">The data stream.</param>
    public PEStreamExtraData(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
        Size = (uint)data.Length;
    }

    /// <summary>
    /// Gets or sets the data stream.
    /// </summary>
    public Stream Data
    {
        get => _data;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _data = value;
            Size = (uint)value.Length;
        }
    }

    public override void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = (uint)Data.Length;
    }

    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        Data = reader.ReadAsStream(Size);
    }

    public override void Write(PEImageWriter writer)
    {
        Data.CopyTo(writer.Stream);
    }
}