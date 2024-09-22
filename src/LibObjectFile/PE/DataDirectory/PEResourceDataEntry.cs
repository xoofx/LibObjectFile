// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource data entry in a PE file.
/// </summary>
public sealed class PEResourceDataEntry : PEResourceEntry
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private object? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDataEntry"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the resource data entry.</param>
    public PEResourceDataEntry(string name) : base(name)
    {
        Data = Stream.Null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDataEntry"/> class with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the resource data entry.</param>
    public PEResourceDataEntry(PEResourceId id) : base(id)
    {
        Data = Stream.Null;
    }

    /// <summary>
    /// Gets or sets the code page used for encoding the data.
    /// </summary>
    /// <remarks>
    /// The code page is used to encode the data when the data is a string.
    /// </remarks>
    public Encoding? CodePage { get; set; }

    /// <summary>
    /// Gets or sets the data associated with the resource data entry.
    /// </summary>
    /// <remarks>
    /// The data can be a string, a stream, or a byte array.
    /// </remarks>
    public object? Data
    {
        get => _data;
        set
        {
            if (value is not string && value is not Stream && value is not byte[])
            {
                throw new ArgumentException("Invalid data type. Expecting a string, a Stream or a byte[]");
            }

            _data = value;
        }
    }

    private protected override unsafe uint ComputeSize()
    {
        uint dataSize = 0;

        if (Data is string text)
        {
            dataSize = (uint)(CodePage?.GetByteCount(text) ?? text.Length * 2);
        }
        else if (Data is Stream stream)
        {
            dataSize = (uint)stream.Length;
        }
        else if (Data is byte[] buffer)
        {
            dataSize = (uint)buffer.Length;
        }

        return (uint)(sizeof(RawImageResourceDataEntry) + dataSize);
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        switch (Data)
        {
            case string text:
                builder.Append($"Data = {text}");
                break;
            case Stream stream:
                builder.Append($"Data = Stream ({stream.Length} bytes)");
                break;
            case byte[] buffer:
                builder.Append($"Data = byte[{buffer.Length}]");
                break;
        }

        return true;
    }
}
