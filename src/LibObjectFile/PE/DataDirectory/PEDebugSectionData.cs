// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

public abstract class PEDebugSectionData : PESectionData
{
    protected PEDebugSectionData()
    {
    }

    public override bool HasChildren => false;
}