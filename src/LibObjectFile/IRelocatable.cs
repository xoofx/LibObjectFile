// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile
{
    public interface IRelocatable
    {
        ulong GetRelocatableValue(ulong relativeOffset, RelocationSize size);

        void SetRelocatableValue(ulong relativeOffset, RelocationSize size);
    }
}