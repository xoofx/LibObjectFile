// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a RSDS debug data.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PEDebugDataRSDS
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEDebugDataRSDS"/> class.
    /// </summary>
    public PEDebugDataRSDS()
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

    public override string ToString() => $"{nameof(PEDebugDataRSDS)} {Guid} {Age} {PdbPath}";
}