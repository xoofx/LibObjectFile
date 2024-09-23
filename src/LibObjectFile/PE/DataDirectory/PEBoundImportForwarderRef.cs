// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents a forwarder reference in the Bound Import Directory of a Portable Executable (PE) file.
/// </summary>
public readonly record struct PEBoundImportForwarderRef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBoundImportForwarderRef"/> struct.
    /// </summary>
    /// <param name="moduleName">The module name of the forwarder reference.</param>
    public PEBoundImportForwarderRef(PEAsciiStringLink moduleName)
    {
        ModuleName = moduleName;
    }

    /// <summary>
    /// Gets the module name of the forwarder reference.
    /// </summary>
    public PEAsciiStringLink ModuleName { get; }
}