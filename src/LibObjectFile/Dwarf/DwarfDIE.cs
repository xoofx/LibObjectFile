// Copyright (c) Alexandre Mutel. All rights reserved.
// This attribute is licensed under the BSD-Clause 2 license.
// See the license.txt attribute in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    public class DwarfDIE : DwarfContainer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DwarfAttribute> _attributes;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DwarfDIE> _children;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DwarfTagEx _tag;

        public DwarfDIE()
        {
            _attributes = new List<DwarfAttribute>();
            _children = new List<DwarfDIE>();
        }

        protected DwarfDIE(DwarfTagEx tag)
        {
            _tag = tag;
        }

        public virtual DwarfTagEx Tag
        {
            get => _tag;
            set => _tag = value;
        }

        public IReadOnlyList<DwarfAttribute> Attributes => _attributes;

        public IReadOnlyList<DwarfDIE> Children => _children;

        public DwarfAbbreviationItem Abbrev { get; internal set; }

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
            _attributes.AddSorted(this, attribute, true);
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

        protected TValue GetAttributeValue<TValue>(DwarfAttributeKind kind)
        {
            foreach (var attr in _attributes)
            {
                if (attr.Kind == kind)
                {
                    return (TValue)attr.ValueAsObject;
                }
            }

            return default;
        }

        protected unsafe TValue? GetAttributeValueOpt<TValue>(DwarfAttributeKind kind) where TValue : unmanaged
        {
            Debug.Assert(sizeof(TValue) <= sizeof(ulong));
            
            foreach (var attr in _attributes)
            {
                if (attr.Kind == kind)
                {
                    ulong localU64 = attr.ValueAsU64;
                    return *(TValue*) &localU64;
                }
            }

            return default;
        }

        protected DwarfConstant? GetAttributeConstantOpt(DwarfAttributeKind kind)
        {
            foreach (var attr in _attributes)
            {
                if (attr.Kind == kind)
                {
                    return new DwarfConstant
                    {
                        AsValue =
                        {
                            U64 = attr.ValueAsU64
                        },
                        AsObject = attr.ValueAsObject
                    };
                }
            }

            return null;
        }

        protected void SetAttributeConstantOpt(DwarfAttributeKind kind, DwarfConstant? cst)
        {
            for (int i = 0; i < _attributes.Count; i++)
            {
                var attr = _attributes[i];
                if (attr.Kind == kind)
                {
                    if (!cst.HasValue)
                    {
                        RemoveAttributeAt(i);
                    }
                    else
                    {
                        var value = cst.Value;
                        attr.ValueAsU64 = value.AsValue.U64;
                        attr.ValueAsObject = value.AsExpression;
                    }
                    return;
                }
            }

            if (cst.HasValue)
            {
                var value = cst.Value;
                AddAttribute(new DwarfAttribute()
                {
                    Kind = kind,
                    ValueAsU64 = value.AsValue.U64,
                    ValueAsObject = value.AsExpression
                });
            }
        }

        protected DwarfLocation? GetAttributeLocationOpt(DwarfAttributeKind kind)
        {
            foreach (var attr in _attributes)
            {
                if (attr.Kind == kind)
                {
                    return new DwarfLocation
                    {
                        AsValue =
                        {
                            U64 = attr.ValueAsU64
                        },
                        AsObject = attr.ValueAsObject
                    };
                }
            }

            return null;
        }

        protected void SetAttributeLocationOpt(DwarfAttributeKind kind, DwarfLocation? cst)
        {
            for (int i = 0; i < _attributes.Count; i++)
            {
                var attr = _attributes[i];
                if (attr.Kind == kind)
                {
                    if (!cst.HasValue)
                    {
                        RemoveAttributeAt(i);
                    }
                    else
                    {
                        var value = cst.Value;
                        attr.ValueAsU64 = value.AsValue.U64;
                        attr.ValueAsObject = value.AsExpression;
                    }
                    return;
                }
            }

            if (cst.HasValue)
            {
                var value = cst.Value;
                AddAttribute(new DwarfAttribute()
                {
                    Kind = kind,
                    ValueAsU64 = value.AsValue.U64,
                    ValueAsObject = value.AsExpression
                });
            }
        }

        public DwarfAttribute FindAttributeByKey(DwarfAttributeKind kind)
        {
            foreach (var attr in _attributes)
            {
                if (attr.Kind == kind)
                {
                    return attr;
                }
            }

            return null;
        }

        protected unsafe void SetAttributeValue<TValue>(DwarfAttributeKind kind, TValue value)
        {
            for (int i = 0; i < _attributes.Count; i++)
            {
                var attr = _attributes[i];
                if (attr.Kind == kind)
                {
                    if (value == null)
                    {
                        RemoveAttributeAt(i);
                    }
                    else
                    {
                        attr.ValueAsObject = value;
                    }
                    return;
                }
            }

            if (value == null) return;
            AddAttribute(new DwarfAttribute() {  Kind = kind, ValueAsObject = value});
        }

        protected void SetAttributeLinkValue<TLink>(DwarfAttributeKind kind, TLink link) where TLink : IObjectFileNodeLink
        {
            for (int i = 0; i < _attributes.Count; i++)
            {
                var attr = _attributes[i];
                if (attr.Kind == kind)
                {
                    if (link == null)
                    {
                        RemoveAttributeAt(i);
                    }
                    else
                    {
                        attr.ValueAsU64 = link.GetRelativeOffset();
                        attr.ValueAsObject = link.GetObjectFileNode();
                    }
                    return;
                }
            }

            AddAttribute(new DwarfAttribute()
            {
                Kind = kind, 
                ValueAsU64 = link.GetRelativeOffset(),
                ValueAsObject = link.GetObjectFileNode()
            });
        }

        protected unsafe void SetAttributeValueOpt<TValue>(DwarfAttributeKind kind, TValue? value) where TValue : unmanaged
        {
            Debug.Assert(sizeof(TValue) <= sizeof(ulong));

            for (int i = 0; i < _attributes.Count; i++)
            {
                var attr = _attributes[i];
                if (attr.Kind == kind)
                {
                    if (!value.HasValue)
                    {
                        RemoveAttributeAt(i);
                    }
                    else
                    {
                        ulong valueU64 = 0;
                        *((TValue*) &valueU64) = value.Value;
                        attr.ValueAsU64 = valueU64;
                        attr.ValueAsObject = null;
                    }
                    return;
                }
            }

            if (value.HasValue)
            {
                var attr = new DwarfAttribute() {Kind = kind};
                ulong valueU64 = 0;
                *((TValue*)&valueU64) = value.Value;
                attr.ValueAsU64 = valueU64;
                AddAttribute(attr);
            }
        }
    }
}