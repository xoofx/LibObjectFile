// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the way a value is calculated (used by <see cref="ElfObjectFilePart.OffsetKind"/> and <see cref="ElfObjectFilePart.SizeKind"/>
    /// </summary>
    public enum ElfValueKind
    {
        /// <summary>
        /// The associated value is automatically calculated by the system.
        /// </summary>
        Auto,

        /// <summary>
        /// The associated value is set manually.
        /// </summary>
        Manual,
    }
}