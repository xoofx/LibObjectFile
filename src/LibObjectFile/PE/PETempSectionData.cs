// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

internal sealed class PETempSectionData() : PESectionData(false)
{
    public static readonly PETempSectionData Instance = new ();
}