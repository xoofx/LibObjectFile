// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for a raw data directory.
/// </summary>
public abstract class PERawDataDirectory : PEDataDirectory
{
    private byte[] _rawData;

    /// <summary>
    /// Initializes a new instance of the <see cref="PERawDataDirectory"/> class.
    /// </summary>
    private protected PERawDataDirectory(PEDataDirectoryKind kind, int minSize) : base(kind)
    {
        _rawData = new byte[minSize];
        RawDataSize = (uint)minSize;
    }

    /// <summary>
    /// Gets the raw data of the Load Configuration Directory.
    /// </summary>
    public byte[] RawData => _rawData;

    /// <summary>
    /// Gets the config size.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetRawDataSize"/> to set the config size.
    /// </remarks>
    public uint RawDataSize { get; private set; } 

    /// <summary>
    /// Sets the config size.
    /// </summary>
    /// <param name="value">The new config size.</param>
    /// <remarks>
    /// The underlying buffer <see cref="RawData"/> will be resized if necessary.
    /// </remarks>
    public virtual void SetRawDataSize(uint value)
    {
        if (value > _rawData.Length)
        {
            var rawData = new byte[value];
            _rawData.CopyTo(rawData, 0);
            _rawData = rawData;
        }
        else if (value < _rawData.Length)
        {
            // Clear the rest of the buffer
            var span = _rawData.AsSpan().Slice((int)value);
            span.Fill(0);
        }

        RawDataSize = value;
    }
    
    /// <inheritdoc/>
    public sealed override void Read(PEImageReader reader)
    {
        var size = (int)Size;
        if (_rawData.Length < size)
        {
            _rawData = new byte[size];
        }

        reader.Position = Position;
        int read = reader.Read(_rawData.AsSpan().Slice(0, size));
        if (read != size)
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading additional data in {nameof(PERawDataDirectory)}");
            return;
        }

        RawDataSize = (uint)size;

        HeaderSize = ComputeHeaderSize(reader);
    }

    /// <inheritdoc/>
    public sealed override void Write(PEImageWriter writer)
    {
        var rawDataSize = RawDataSize;
        var span = _rawData.AsSpan().Slice(0, (int)rawDataSize);
        writer.Write(span);
    }
    
    /// <inheritdoc/>
    public override int ReadAt(uint offset, Span<byte> destination) => DataUtils.ReadAt(_rawData, offset, destination);

    /// <inheritdoc/>
    public override void WriteAt(uint offset, ReadOnlySpan<byte> source) => DataUtils.WriteAt(_rawData, offset, source);

    public override bool CanReadWriteAt(uint offset, uint size) => offset + size <= RawDataSize;

    protected sealed override uint ComputeHeaderSize(PELayoutContext context)
        // Size if the first field of the Load Configuration Directory
        => (uint)RawDataSize;
}