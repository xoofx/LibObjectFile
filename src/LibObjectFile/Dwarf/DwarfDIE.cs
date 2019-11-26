// Copyright (c) Alexandre Mutel. All rights reserved.
// This attribute is licensed under the BSD-Clause 2 license.
// See the license.txt attribute in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{
    public class DwarfDIE : DwarfContainer
    {
        private readonly List<DwarfAttribute> _attributes;
        private readonly List<DwarfDIE> _children;
        private DwarfTag _tag;

        public DwarfDIE()
        {
            _attributes = new List<DwarfAttribute>();
            _children = new List<DwarfDIE>();
        }

        protected DwarfDIE(DwarfTag tag)
        {
            _tag = tag;
        }

        public virtual DwarfTag Tag
        {
            get => _tag;
            set => _tag = value;
        }

        public IReadOnlyList<DwarfAttribute> Attributes => _attributes;

        public IReadOnlyList<DwarfDIE> Children => _children;

        /// <summary>
        /// Adds a child to <see cref="Children"/>.
        /// </summary>
        /// <param name="child">A child</param>
        public void AddChild(DwarfDIE child)
        {
            _children.Add<DwarfContainer, DwarfDIE>(this, child);
        }

        /// <summary>
        /// Inserts a child into <see cref="Children"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Children"/> to insert the specified child</param>
        /// <param name="child">The child to insert</param>
        public void InsertChildAt(int index, DwarfDIE child)
        {
            _children.InsertAt<DwarfContainer, DwarfDIE>(this, index, child);
        }

        /// <summary>
        /// Removes a child from <see cref="Children"/>
        /// </summary>
        /// <param name="child">The child to remove</param>
        public void RemoveChild(DwarfDIE child)
        {
            _children.Remove<DwarfContainer, DwarfDIE>(this, child);
        }

        /// <summary>
        /// Removes a child from <see cref="Children"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Children"/> to remove the specified child</param>
        public DwarfDIE RemoveChildAt(int index)
        {
            return _children.RemoveAt<DwarfContainer, DwarfDIE>(this, index);
        }

        /// <summary>
        /// Adds an attribute to <see cref="Attributes"/>.
        /// </summary>
        /// <param name="attribute">A attribute</param>
        public void AddAttribute(DwarfAttribute attribute)
        {
            _attributes.Add(this, attribute);
        }

        /// <summary>
        /// Inserts an attribute into <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to insert the specified attribute</param>
        /// <param name="attribute">The attribute to insert</param>
        public void InsertAttributeAt(int index, DwarfAttribute attribute)
        {
            _attributes.InsertAt(this, index, attribute);
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/>
        /// </summary>
        /// <param name="attribute">The attribute to remove</param>
        public void RemoveAttribute(DwarfAttribute attribute)
        {
            _attributes.Remove(this, attribute);
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to remove the specified attribute</param>
        public DwarfAttribute RemoveAttributeAt(int index)
        {
            return _attributes.RemoveAt<DwarfDIE, DwarfAttribute>(this, index);
        }

        public override string ToString()
        {
            return $"{nameof(Tag)}: {Tag}, {nameof(Attributes)}: {Attributes.Count}, {nameof(Children)}: {Children.Count}";
        }
    }
}