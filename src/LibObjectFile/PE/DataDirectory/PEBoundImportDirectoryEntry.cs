// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Represents an entry in the Bound Import Directory of a Portable Executable (PE) file.
/// </summary>
public sealed class PEBoundImportDirectoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBoundImportDirectoryEntry"/> class.
    /// </summary>
    public PEBoundImportDirectoryEntry()
    {
        ForwarderRefs = new List<PEBoundImportForwarderRef>();
    }

    /// <summary>
    /// Gets or sets the module name for this entry.
    /// </summary>
    public PEAsciiStringLink ModuleName { get; set; }

    /// <summary>
    /// Gets the list of forwarder references for this entry.
    /// </summary>
    public List<PEBoundImportForwarderRef> ForwarderRefs { get; }

    /// <summary>
    /// Gets the size of this entry in the Bound Import Directory.
    /// </summary>
    internal unsafe uint Size => (uint)(sizeof(RawPEBoundImportDirectory) + ForwarderRefs.Count * sizeof(RawPEBoundImportForwarderRef));
}