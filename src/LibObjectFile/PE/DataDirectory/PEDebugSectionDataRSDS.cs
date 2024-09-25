// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a RSDS debug data.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PEDebugSectionDataRSDS : PEDebugSectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEDebugSectionDataRSDS"/> class.
    /// </summary>
    public PEDebugSectionDataRSDS()
    {
        Guid = Guid.Empty;
        Age = 0;
        PdbPath = string.Empty;
    }

    /// <summary>
    /// Gets or sets the GUID of the PDB.
    /// </summary>
    public Guid Guid { get; set; }

    /// <summary>
    /// Gets or sets the age of the PDB.
    /// </summary>
    public uint Age { get; set; }

    /// <summary>
    /// Gets or sets the path of the PDB.
    /// </summary>
    public string PdbPath { get; set; }
    
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var size = (int)Size;
        var buffer = ArrayPool<byte>.Shared.Rent((int)Size);
        var span = buffer.AsSpan(0, size);
        try
        {
            var read = reader.Read(span);
            if (read != span.Length)
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read PEDebugDataRSDS");
                return;
            }

            var signature = MemoryMarshal.Read<uint>(span);
            if (signature != 0x53445352)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidDebugDataRSDSSignature, $"Invalid signature for PEDebugDataRSDS");
                return;
            }

            var pdbPath = span.Slice(sizeof(uint) + sizeof(Guid) + sizeof(uint));
            var indexOfZero = pdbPath.IndexOf((byte)0);
            if (indexOfZero < 0)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidDebugDataRSDSPdbPath, $"Invalid PDB path for PEDebugDataRSDS");
                return;
            }

            Guid = MemoryMarshal.Read<Guid>(span.Slice(4));
            Age = MemoryMarshal.Read<uint>(span.Slice(sizeof(uint) + sizeof(Guid)));
            PdbPath = System.Text.Encoding.UTF8.GetString(pdbPath.Slice(0, indexOfZero));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }
        builder.Append($"Guid: {Guid}, Age: {Age}, PdbPath: {PdbPath}");
        return true;
    }
}