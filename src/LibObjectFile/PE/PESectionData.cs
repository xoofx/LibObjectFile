// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for data contained in a <see cref="PESection"/>.
/// </summary>
public abstract class PESectionData : PEObject
{
    protected PESectionData()
    {
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PESection && parent is not PESectionData)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PESection).FullName} or {typeof(PESectionData).FullName}");
        }
    }
}