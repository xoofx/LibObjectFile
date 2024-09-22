// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents an exception function entry in a Portable Executable (PE) file.
/// </summary>
public abstract class PEExceptionFunctionEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEExceptionFunctionEntry"/> class with the specified begin address.
    /// </summary>
    /// <param name="beginAddress">The begin address of the exception function entry.</param>
    internal PEExceptionFunctionEntry(PESectionDataLink beginAddress)
    {
        BeginAddress = beginAddress;
    }

    /// <summary>
    /// Gets or sets the begin address of the exception function entry.
    /// </summary>
    public PESectionDataLink BeginAddress { get; set; }
}
