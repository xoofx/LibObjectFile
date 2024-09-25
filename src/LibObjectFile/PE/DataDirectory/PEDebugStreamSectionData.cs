// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// A debug stream section data in a Portable Executable (PE) image.
/// </summary>
public class PEDebugStreamSectionData : PEStreamSectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEDebugStreamSectionData"/> class.
    /// </summary>
    public PEDebugStreamSectionData()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEDebugStreamSectionData"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the data of this section data.</param>
    public PEDebugStreamSectionData(Stream stream) : base(stream)
    {
    }
}