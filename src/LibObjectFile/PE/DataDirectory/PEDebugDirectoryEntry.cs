// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a debug directory entry in a Portable Executable (PE) file.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PEDebugDirectoryEntry
{
    /// <summary>
    /// Gets or sets the characteristics of the debug directory entry.
    /// </summary>
    public uint Characteristics { get; set; }

    /// <summary>
    /// Gets or sets the time and date stamp of the debug directory entry.
    /// </summary>
    public uint TimeDateStamp { get; set; }

    /// <summary>
    /// Gets or sets the major version of the debug directory entry.
    /// </summary>
    public ushort MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets the minor version of the debug directory entry.
    /// </summary>
    public ushort MinorVersion { get; set; }

    /// <summary>
    /// Gets or sets the type of the debug directory entry.
    /// </summary>
    public PEDebugType Type { get; set; }

    /// <summary>
    /// Gets or sets the data link of the debug directory entry.
    /// </summary>
    public PEBlobDataLink DataLink { get; set; }

    /// <summary>
    /// Try to get the CodeView (RSDS) information from this debug directory entry.
    /// </summary>
    /// <param name="debugRSDS">The CodeView (RSDS) information if the entry is of type <see cref="PEDebugKnownType.CodeView"/>.</param>
    /// <returns><c>true</c> if the CodeView (RSDS) information has been successfully retrieved.</returns>
    /// <remarks>
    /// The entry must be of type <see cref="PEDebugKnownType.CodeView"/> to be able to get the CodeView information. The data behind the <see cref="DataLink"/> must be a CodeView (RSDS) format.
    /// </remarks>
    public unsafe bool TryGetDebugRSDS([NotNullWhen(true)] out PEDebugDataRSDS? debugRSDS)
    {
        debugRSDS = null;
        if (Type.Value != (uint)PEDebugKnownType.CodeView)
        {
            return false;
        }

        var dataLink = DataLink;
        if (dataLink.Container == null)
        {
            return false;
        }

        var container = dataLink.Container;
        var buffer = ArrayPool<byte>.Shared.Rent((int)dataLink.Size);
        var span = buffer.AsSpan(0, (int)dataLink.Size);
        try
        {
            var read = container.ReadAt(DataLink.RVO, span);
            if (read != span.Length)
            {
                return false;
            }

            var signature = MemoryMarshal.Read<uint>(span);
            if (signature != 0x53445352)
            {
                return false;
            }

            var pdbPath = span.Slice(sizeof(uint) + sizeof(Guid) + sizeof(uint));
            var indexOfZero = pdbPath.IndexOf((byte)0);
            if (indexOfZero < 0)
            {
                return false;
            }

            debugRSDS = new PEDebugDataRSDS
            {
                Guid = MemoryMarshal.Read<Guid>(span.Slice(4)),
                Age = MemoryMarshal.Read<uint>(span.Slice(sizeof(uint) + sizeof(Guid))),
                PdbPath = System.Text.Encoding.UTF8.GetString(pdbPath.Slice(0, indexOfZero))
            };

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public override string ToString() => $"{nameof(PEDebugDirectoryEntry)} {Type} {DataLink}";
}