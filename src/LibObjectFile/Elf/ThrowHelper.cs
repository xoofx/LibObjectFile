// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    internal static class ThrowHelper
    {
        public static InvalidOperationException InvalidEnum(object v)
        {
            return new InvalidOperationException($"Invalid Enum {v.GetType()}.{v}");
        }
    }
}