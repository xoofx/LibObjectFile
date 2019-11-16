// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Base class for an <see cref="ElfSection"/> and <see cref="ElfSegment"/>.
    /// </summary>
    public abstract class ElfObjectFilePart
    {
        private ulong _offset;
        private ulong _size;

        /// <summary>
        /// Gets or sets the offset of this section or segment in the parent <see cref="ElfObjectFile"/>.
        /// </summary>
        public virtual ulong Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>
        /// Gets or sets the way the <see cref="Offset"/> is calculated (e.g auto or manual) of this section or segment.
        /// </summary>
        public virtual ElfValueKind OffsetKind { get; set; }

        /// <summary>
        /// Gets the containing <see cref="ElfObjectFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="ElfObjectFile"/>.
        /// </summary>
        public ElfObjectFile Parent { get; internal set; }
        
        /// <summary>
        /// Index within <see cref="ElfObjectFile.Segments"/> or <see cref="ElfObjectFile.Sections"/>.
        /// </summary>
        public uint Index { get; internal set; }

        /// <summary>
        /// Gets or sets the size of this section or segment in the parent <see cref="ElfObjectFile"/>.
        /// </summary>
        public virtual ulong Size
        {
            get => SizeKind == ElfValueKind.Auto ? GetSizeAuto() : _size;
            set
            {
                SizeKind = ElfValueKind.Absolute;
                _size = value;
            }
        }

        /// <summary>
        /// Gets or sets the way the <see cref="Size"/> is calculated (e.g auto or manual) of this section or segment.
        /// </summary>
        public virtual ElfValueKind SizeKind { get; set; }

        /// <summary>
        /// A method to implement when <see cref="SizeKind"/> is <see cref="ElfValueKind.Auto"/>
        /// </summary>
        /// <returns>The size of this section or segment automatically calculated.</returns>
        protected virtual ulong GetSizeAuto() => 0;

        /// <summary>
        /// Checks if the specified offset is contained by this instance.
        /// </summary>
        /// <param name="offset">The offset to check if it belongs to this instance.</param>
        /// <returns><c>true</c> if the offset is within the segment or section range.</returns>
        public bool Contains(ulong offset)
        {
            return offset >= _offset && offset < _offset + Size;
        }

        /// <summary>
        /// Checks this instance contains either the beginning or the end of the specified section or segment.
        /// </summary>
        /// <param name="part">The specified section or segment.</param>
        /// <returns><c>true</c> if the either the offset or end of the part is within this segment or section range.</returns>
        public bool Contains(ElfObjectFilePart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return Contains(part.Offset) || part.Size != 0 && Contains(part.Offset + part.Size - 1);
        }

        /// <summary>
        /// Verifies this instance.
        /// </summary>
        /// <param name="diagnostics">The diagnostics.</param>
        public virtual void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }
    }
}