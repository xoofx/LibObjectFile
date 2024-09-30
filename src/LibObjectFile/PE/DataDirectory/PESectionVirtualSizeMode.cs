// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines the way the virtual size of a section is computed.
/// </summary>
public enum PESectionVirtualSizeMode
{
    /// <summary>
    /// The virtual size of the section is automatically computed by the raw size of its content without file alignment.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// The virtual size of the section is fixed.
    /// </summary>
    /// <remarks>
    /// This is usually the case when the virtual size is requested to be bigger than the raw size (e.g. uninitialized data).
    /// </remarks>
    Fixed = 1,
}