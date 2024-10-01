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
    private PEObjectBase? _data;

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
    /// Gets or sets the associated section data to this debug entry.
    /// </summary>
    /// <remarks>
    /// This is set when the data is located inside a section. Otherwise <see cref="ExtraData"/> might be set.
    /// </remarks>
    public PESectionData? SectionData
    {
        get => _data as PESectionData;
        set => _data = value;
    }

    /// <summary>
    /// Gets or sets the associated blob data outside a section (e.g. in <see cref="PEFile.ExtraDataBeforeSections"/> or <see cref="PEFile.ExtraDataAfterSections"/>).
    /// </summary>
    /// <remarks>
    /// This is set when the data is located outside a section. Otherwise <see cref="SectionData"/> might be set.
    /// </remarks>
    public PEExtraData? ExtraData
    {
        get => _data as PEExtraData;
        set => _data = value;
    }

    public override string ToString() => $"{nameof(PEDebugDirectoryEntry)} {Type} {_data}";
}