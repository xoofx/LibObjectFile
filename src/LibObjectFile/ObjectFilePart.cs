// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile
{
    /// <summary>
    /// Base class for a part of a file.
    /// </summary>
    public abstract class ObjectFilePart<TParentFile, TObjectFilePart> where TObjectFilePart : ObjectFilePart<TParentFile, TObjectFilePart>
    {
        private ulong _offset;
        private ulong _size;

        /// <summary>
        /// Gets or sets the offset of this section or segment in the parent <see cref="TParentFile"/>.
        /// </summary>
        public virtual ulong Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>
        /// Gets or sets the way the <see cref="Offset"/> is calculated (e.g auto or manual) of this section or segment.
        /// </summary>
        public virtual ValueKind OffsetKind { get; set; }

        /// <summary>
        /// Gets the containing <see cref="TParentFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="TParentFile"/>.
        /// </summary>
        public TParentFile Parent { get; internal set; }
        
        /// <summary>
        /// Index within the containing list in the <see cref="TParentFile"/>
        /// </summary>
        public uint Index { get; internal set; }

        /// <summary>
        /// Gets or sets the size of this section or segment in the parent <see cref="TParentFile"/>.
        /// </summary>
        public virtual ulong Size
        {
            get => SizeKind == ValueKind.Auto ? GetSizeAuto() : _size;
            set
            {
                SizeKind = ValueKind.Manual;
                _size = value;
            }
        }

        /// <summary>
        /// Gets or sets the way the <see cref="Size"/> is calculated (e.g auto or manual) of this section or segment.
        /// </summary>
        public virtual ValueKind SizeKind { get; set; }

        /// <summary>
        /// A method to implement when <see cref="SizeKind"/> is <see cref="ValueKind.Auto"/>
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
        public bool Contains(TObjectFilePart part)
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