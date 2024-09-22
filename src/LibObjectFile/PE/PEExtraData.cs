// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Base class for extra data in a PE file accessible via <see cref="PEFile.ExtraData"/>.
/// </summary>
public abstract class PEExtraData : PEObjectBase
{
    protected PEExtraData()
    {
    }
}