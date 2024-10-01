﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

internal struct RawExceptionFunctionEntryARM
{
    public RVA BeginAddress;
    public uint UnwindData;
}