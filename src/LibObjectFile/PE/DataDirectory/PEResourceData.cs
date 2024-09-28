// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A section data that contains a resource data.
/// </summary>
public sealed class PEResourceData : PEStreamSectionData
{
    public PEResourceData()
    {
        RequiredPositionAlignment = 4;
        RequiredSizeAlignment = 4;
    }
}