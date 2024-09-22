// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Load Configuration Directory for a PE file.
/// </summary>
public sealed class PELoadConfigDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory"/> class.
    /// </summary>
    public PELoadConfigDirectory() : base(PEDataDirectoryKind.LoadConfig)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PELoadConfigDirectory"/> class with the specified bitness.
    /// </summary>
    /// <param name="is32Bits">A value indicating whether the directory is 32-bit or 64-bit.</param>
    public unsafe PELoadConfigDirectory(bool is32Bits) : base(PEDataDirectoryKind.LoadConfig)
    {
        Is32Bits = is32Bits;
        LoadConfigDirectory32.Size = (uint)sizeof(PELoadConfigDirectory32);
        LoadConfigDirectory64.Size = (uint)sizeof(PELoadConfigDirectory64);
    }

    /// <summary>
    /// Gets a value indicating whether the directory is 32-bit or 64-bit.
    /// </summary>
    public bool Is32Bits { get; private set; }

    /// <summary>
    /// Gets or sets the 32-bit Load Configuration Directory.
    /// </summary>
    public PELoadConfigDirectory32 LoadConfigDirectory32;

    /// <summary>
    /// Gets or sets the 64-bit Load Configuration Directory.
    /// </summary>
    public PELoadConfigDirectory64 LoadConfigDirectory64;

    /// <summary>
    /// Gets or sets the additional data after the Load Configuration Directory.
    /// </summary>
    public byte[]? AdditionalData { get; set; }

    /// <inheritdoc/>
    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        return (Is32Bits ? LoadConfigDirectory32.Size : LoadConfigDirectory64.Size) + (uint)(AdditionalData?.Length ?? 0);
    }

    /// <inheritdoc/>
    public override unsafe void Read(PEImageReader reader)
    {
        Is32Bits = reader.File.IsPE32;
        reader.Position = Position;

        int size = (int)Size;
        if (Is32Bits)
        {
            size = Math.Min(size, sizeof(PELoadConfigDirectory32));
            if (!reader.TryReadData(size, out LoadConfigDirectory32))
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PELoadConfigDirectory32)}");
                return;
            }
        }
        else
        {
            size = Math.Min(size, sizeof(PELoadConfigDirectory64));
            if (!reader.TryReadData(size, out LoadConfigDirectory64))
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PELoadConfigDirectory64)}");
                return;
            }
        }

        // If we have more data than expected, we read it as additional data
        if ((int)Size > size)
        {
            AdditionalData = new byte[(int)Size - size];
            int read = reader.Read(AdditionalData);

            if (read != AdditionalData.Length)
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading additional data in {nameof(PELoadConfigDirectory)}");
                return;
            }
        }
        
        HeaderSize = ComputeHeaderSize(reader);
    }

    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override int ReadAt(uint offset, Span<byte> destination) => DataUtils.ReadAt(Is32Bits, ref LoadConfigDirectory32, ref LoadConfigDirectory64, offset, destination);

    /// <inheritdoc/>
    public override void WriteAt(uint offset, ReadOnlySpan<byte> source) => DataUtils.WriteAt(Is32Bits, ref LoadConfigDirectory32, ref LoadConfigDirectory64, offset, source);
}
