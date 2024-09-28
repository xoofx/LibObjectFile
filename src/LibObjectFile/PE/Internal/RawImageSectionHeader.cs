// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

/// <summary>
/// Represents the section header in a PE (Portable Executable) file format.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
[DebuggerDisplay("{NameAsString,nq}")]
internal unsafe struct RawImageSectionHeader
{
    /// <summary>
    /// An 8-byte name for the section.
    /// </summary>
    public fixed byte Name[8];

    /// <summary>
    /// Returns the name of the section as a string.
    /// </summary>
    public string NameAsString
    {
        get
        {
            var span = MemoryMarshal.CreateSpan(ref Name[0], 8);
            int length = span.IndexOf((byte)0);
            if (length >= 0)
            {
                span = span.Slice(0, length);
            }

            return Encoding.ASCII.GetString(span);
        }
    }

    /// <summary>
    /// The total size of the section when loaded into memory.
    /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded.
    /// </summary>
    public uint VirtualSize;

    /// <summary>
    /// The physical address or the size of the section's data in memory.
    /// This field is an alias for <see cref="VirtualSize"/>.
    /// </summary>
    public uint PhysicalAddress
    {
        get => VirtualSize;
        set => VirtualSize = value;
    }

    /// <summary>
    /// The address of the first byte of the section when loaded into memory, relative to the image base.
    /// </summary>
    public RVA RVA;

    /// <summary>
    /// The size of the section's raw data in the file, in bytes.
    /// </summary>
    public uint SizeOfRawData;

    /// <summary>
    /// The file pointer to the first page of the section's raw data.
    /// </summary>
    public uint PointerToRawData;

    /// <summary>
    /// The file pointer to the beginning of the relocation entries for the section, if present.
    /// </summary>
    public uint PointerToRelocations;

    /// <summary>
    /// The file pointer to the beginning of the line-number entries for the section, if present.
    /// </summary>
    public uint PointerToLineNumbers;

    /// <summary>
    /// The number of relocation entries for the section.
    /// </summary>
    public ushort NumberOfRelocations;

    /// <summary>
    /// The number of line-number entries for the section.
    /// </summary>
    public ushort NumberOfLineNumbers;

    /// <summary>
    /// Flags that describe the characteristics of the section.
    /// </summary>
    public System.Reflection.PortableExecutable.SectionCharacteristics Characteristics;
}