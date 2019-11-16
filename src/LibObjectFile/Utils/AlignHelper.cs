// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils
{
    public static class AlignHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AlignToUpper(ulong value, ulong align)
        {
            var nextValue = ((value + align - 1) / align) * align;
            return nextValue;
        }
    }
}