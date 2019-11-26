// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile
{
    public enum RelocationSize
    {
        I8,
        I16,
        I32,
        I64,
    }

    public interface IRelocatable
    {
        ulong GetRelocatableValue(ulong relativeOffset, RelocationSize size);

        void SetRelocatableValue(ulong relativeOffset, RelocationSize size);
    }
    
    public abstract class ObjectFilePart
    {
        private object _parent;
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
        /// Gets the containing parent.
        /// </summary>
        public virtual object Parent
        {
            get => _parent;

            internal set
            {
                if (value == null)
                {
                    _parent = null;
                }
                else
                {
                    ValidateParent(value);
                }

                _parent = value;
            }
        }

        protected virtual void ValidateParent(object parent)
        {
        }

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
        public bool Contains(ObjectFilePart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return Contains((ulong)part.Offset) || part.Size != 0 && Contains((ulong)(part.Offset + part.Size - 1));
        }

        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <returns>The result of the diagnostics</returns>
        public DiagnosticBag Verify()
        {
            var diagnostics = new DiagnosticBag();
            Verify(diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        public virtual void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>
        /// Update and calculate the layout of this file.
        /// </summary>
        public void UpdateLayout()
        {
            var diagnostics = new DiagnosticBag();
            TryUpdateLayout(diagnostics);
            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected error while updating the layout of this instance", diagnostics);
            }
        }

        /// <summary>
        /// Tries to update and calculate the layout of the sections, segments and <see cref="Layout"/>.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        /// <returns><c>true</c> if the calculation of the layout is successful. otherwise <c>false</c></returns>
        public virtual bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            return true;
        }
    }

    /// <summary>
    /// Base class for a part of a file.
    /// </summary>
    public abstract class ObjectFilePart<TParentFile> : ObjectFilePart
    {
        protected override void ValidateParent(object parent)
        {
            if (!(parent is TParentFile))
            {
                throw new ArgumentException($"Parent must inherit from type {nameof(TParentFile)}");
            }
        }
        
        /// <summary>
        /// Gets the containing <see cref="TParentFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="TParentFile"/>.
        /// </summary>
        public new TParentFile Parent
        {
            get => (TParentFile) base.Parent;
            internal set => base.Parent = value;
        }

    }

    public static class ObjectFileExtensions
    {
        /// <summary>
        /// Adds an attribute to <see cref="Attributes"/>.
        /// </summary>
        /// <param name="element">A attribute</param>
        public static void Add<TParent, TChild>(this List<TChild> list, TParent parent, TChild element) where TChild : ObjectFilePart<TParent> where TParent : class
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Parent != null)
            {
                if (element.Parent == parent) throw new InvalidOperationException($"Cannot add the {element.GetType()} as it is already added");
                if (element.Parent != parent) throw new InvalidOperationException($"Cannot add the {element.GetType()}  as it is already added to another {parent.GetType()} instance");
            }

            element.Parent = parent;
            element.Index = (uint)list.Count;
            list.Add(element);
        }

        /// <summary>
        /// Inserts an attribute into <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to insert the specified attribute</param>
        /// <param name="element">The attribute to insert</param>
        public static void InsertAt<TParent, TChild>(this List<TChild> list, TParent parent, int index, TChild element) where TChild : ObjectFilePart<TParent> where TParent : class
        {
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {list.Count}");
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Parent != null)
            {
                if (element.Parent == parent) throw new InvalidOperationException($"Cannot add the {element.GetType()} as it is already added");
                if (element.Parent != parent) throw new InvalidOperationException($"Cannot add the {element.GetType()}  as it is already added to another {parent.GetType()} instance");
            }

            element.Index = (uint)index;
            list.Insert(index, element);
            element.Parent = parent;

            // Update the index of following attributes
            for (int i = index + 1; i < list.Count; i++)
            {
                var nextAttribute = list[i];
                nextAttribute.Index++;
            }
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/>
        /// </summary>
        /// <param name="child">The attribute to remove</param>
        public static void Remove<TParent, TChild>(this List<TChild> list, TParent parent, TChild child) where TChild : ObjectFilePart<TParent> where TParent : class
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (!ReferenceEquals(child.Parent, parent))
            {
                throw new InvalidOperationException($"Cannot remove the {nameof(TChild)} as it is not part of this {parent.GetType()} instance");
            }

            var i = (int)child.Index;
            list.RemoveAt(i);
            child.Index = 0;

            // Update indices for other sections
            for (int j = i + 1; j < list.Count; j++)
            {
                var nextEntry = list[j];
                nextEntry.Index--;
            }

            child.Parent = null;
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to remove the specified attribute</param>
        public static TChild RemoveAt<TParent, TChild>(this List<TChild> list, TParent parent, int index) where TChild : ObjectFilePart<TParent> where TParent : class
        {
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {list.Count}");
            var child = list[index];
            Remove<TParent, TChild>(list, parent, child);
            return child;
        }
    }
}