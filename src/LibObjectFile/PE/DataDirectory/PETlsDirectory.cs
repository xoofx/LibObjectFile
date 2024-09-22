// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public sealed class PETlsDirectory : PEDataDirectory
{
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    public PETlsDirectory() : base(PEDataDirectoryKind.Tls)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    /// <param name="is32Bits">True if the PE file is 32 bits, false if it is 64 bits.</param>
    public PETlsDirectory(bool is32Bits) : base(PEDataDirectoryKind.Tls)
    {
        Is32Bits = is32Bits;
    }

    /// <summary>
    /// Gets a boolean indicating if this TLS directory is for a 32 bits PE file.
    /// </summary>
    public bool Is32Bits { get; private set; }


    /// <summary>
    /// Gets the TLS directory for a PE32 file.
    /// </summary>
    public PETlsDirectory32 TlsDirectory32;
    

    /// <summary>
    /// Gets the TLS directory for a PE64 file.
    /// </summary>
    public PETlsDirectory64 TlsDirectory64;


    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        return Is32Bits ? (uint)sizeof(PETlsDirectory32) : (uint)sizeof(PETlsDirectory64);
    }
    
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        Is32Bits = reader.File.IsPE32;

        if (Is32Bits)
        {
            if (!reader.TryReadData(sizeof(PETlsDirectory32), out TlsDirectory32))
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PETlsDirectory)}");
                return;
            }
        }
        else
        {
            if (!reader.TryReadData(sizeof(PETlsDirectory64), out TlsDirectory64))
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PETlsDirectory)}");
                return;
            }
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    public override void Write(PEImageWriter writer)
    {
    }

    public override int ReadAt(uint offset, Span<byte> destination) => DataUtils.ReadAt(Is32Bits, ref TlsDirectory32, ref TlsDirectory64, offset, destination);

    public override void WriteAt(uint offset, ReadOnlySpan<byte> source) => DataUtils.WriteAt(Is32Bits, ref TlsDirectory32, ref TlsDirectory64, offset, source);
}