// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

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
    
    /// <inheritdoc/>
    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PEResourceDirectory)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PEResourceDirectory).FullName}");
        }
    }
}