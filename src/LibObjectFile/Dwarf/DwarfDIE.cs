// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

public class DwarfDIE : DwarfContainer
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly SortedObjectList<DwarfAttribute> _attributes;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ObjectList<DwarfDIE> _children;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private DwarfTagEx _tag;

    /// <summary>
    /// The current line program table when reading.
    /// </summary>
    internal DwarfLineProgramTable? CurrentLineProgramTable;

    public DwarfDIE()
    {
        _attributes = new SortedObjectList<DwarfAttribute>(this);
        _children = new ObjectList<DwarfDIE>(this);
    }

    public virtual DwarfTagEx Tag
    {
        get => _tag;
        set => _tag = value;
    }

    public SortedObjectList<DwarfAttribute> Attributes => _attributes;

    public ObjectList<DwarfDIE> Children => _children;

    public DwarfAbbreviationItem? Abbrev { get; internal set; }


    public override void Verify(DwarfVerifyContext context)
    {
        foreach (var attr in _attributes)
        {
            attr.Verify(context);
        }

        foreach (var child in _children)
        {
            child.Verify(context);
        }
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(Tag)}: {Tag}, {nameof(Attributes)}: {Attributes.Count}, {nameof(Children)}: {Children.Count}, ");
        base.PrintMembers(builder);
        return true;
    }

    protected TValue? GetAttributeValue<TValue>(DwarfAttributeKind kind)
    {
        foreach (var attr in _attributes)
        {
            if (attr.Kind == kind)
            {
                return (TValue?)attr.ValueAsObject;
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
                    Attributes.RemoveAt(i);
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
            Attributes.Add(new DwarfAttribute()
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
                    Attributes.RemoveAt(i);
                }
                else
                {
                    var value = cst.Value;
                    attr.ValueAsU64 = value.AsValue.U64;
                    attr.ValueAsObject = value.AsObject;
                }
                return;
            }
        }

        if (cst.HasValue)
        {
            var value = cst.Value;
            Attributes.Add(new DwarfAttribute()
            {
                Kind = kind,
                ValueAsU64 = value.AsValue.U64,
                ValueAsObject = value.AsObject
            });
        }
    }

    public DwarfAttribute? FindAttributeByKey(DwarfAttributeKind kind)
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

    protected unsafe void SetAttributeValue<TValue>(DwarfAttributeKind kind, TValue? value)
    {
        for (int i = 0; i < _attributes.Count; i++)
        {
            var attr = _attributes[i];
            if (attr.Kind == kind)
            {
                if (value == null)
                {
                    Attributes.RemoveAt(i);
                }
                else
                {
                    attr.ValueAsObject = value;
                }
                return;
            }
        }

        if (value == null) return;
        Attributes.Add(new DwarfAttribute() {  Kind = kind, ValueAsObject = value});
    }

    //protected void SetAttributeLinkValue<TLink>(DwarfAttributeKind kind, TLink link) where TLink : IObjectFileNodeLink
    //{
    //    for (int i = 0; i < _attributes.Count; i++)
    //    {
    //        var attr = _attributes[i];
    //        if (attr.Kind == kind)
    //        {
    //            if (link == null)
    //            {
    //                RemoveAttributeAt(i);
    //            }
    //            else
    //            {
    //                attr.ValueAsU64 = link.GetRelativeOffset();
    //                attr.ValueAsObject = link.GetObjectFileNode();
    //            }
    //            return;
    //        }
    //    }

    //    AddAttribute(new DwarfAttribute()
    //    {
    //        Kind = kind, 
    //        ValueAsU64 = link.GetRelativeOffset(),
    //        ValueAsObject = link.GetObjectFileNode()
    //    });
    //}

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
                    Attributes.RemoveAt(i);
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
            Attributes.Add(attr);
        }
    }

    public override void UpdateLayout(DwarfLayoutContext layoutContext)
    {
        var abbrev = Abbrev;

        var endOffset = Position;
        if (abbrev is null)
        {
            throw new InvalidOperationException("Abbreviation is not set");
        }
        endOffset += DwarfHelper.SizeOfULEB128(abbrev.Code); // WriteULEB128(abbreviationItem.Code);

        foreach (var attr in _attributes)
        {
            attr.Position = endOffset;
            attr.UpdateLayout(layoutContext);
            endOffset += attr.Size;
        }

        if (abbrev.HasChildren)
        {
            foreach (var child in _children)
            {
                child.Position = endOffset;
                child.UpdateLayout(layoutContext);
                endOffset += child.Size;
            }

            // Encode abbreviation 0 code
            endOffset += DwarfHelper.SizeOfULEB128(0);
        }

        Size = endOffset - Position;
    }

    public override void Read(DwarfReader reader)
    {
        // Store map offset to DIE to resolve references
        reader.PushDIE(this);

        // Console.WriteLine($" <{level}><{die.Offset:x}> Abbrev Number: {abbreviationCode} ({die.Tag})");

        if (Abbrev is null)
        {
            throw new InvalidOperationException("Abbreviation is not set");
        }

        var descriptors = Abbrev.Descriptors;
        if (descriptors.Length > 0)
        {
            for (int i = 0; i < descriptors.Length; i++)
            {
                reader.CurrentAttributeDescriptor = descriptors[i];
                    
                var attribute = new DwarfAttribute()
                {
                    Position = reader.Position,
                };

                attribute.Read(reader);

                Attributes.Add(attribute);
            }
        }

        if (Abbrev.HasChildren)
        {
            while (true)
            {
                reader.DIELevel++;
                var child = ReadInstance(reader);
                reader.DIELevel--;
                if (child == null) break;

                Children.Add(child);
            }
        }

        reader.PopDIE();

        Size = reader.Position - Position;
    }

    internal static DwarfDIE? ReadInstance(DwarfReader reader)
    {
        var startDIEOffset = reader.Position;
        var abbreviationCode = reader.ReadULEB128();
        DwarfDIE? die = null;

        if (abbreviationCode != 0)
        {

            if (!reader.CurrentUnit!.Abbreviation!.TryFindByCode(abbreviationCode, out var abbreviationItem))
            {
                throw new InvalidOperationException($"Invalid abbreviation code {abbreviationCode}");
            }

            die = DIEHelper.ConvertTagToDwarfDIE((ushort) abbreviationItem.Tag);
            die.Position = startDIEOffset;
            die.Abbrev = abbreviationItem;
            die.Tag = abbreviationItem.Tag;
            die.Read(reader);
        }

        return die;
    }

    internal void UpdateAbbreviationItem(DwarfLayoutContext context)
    {
        // Initialize the offset of DIE to ulong.MaxValue to make sure that when we have a reference
        // to it, we can detect if it is a forward or backward reference.
        // If it is a backward reference, we will be able to encode the offset
        // otherwise we will have to pad the encoding with NOP (for DwarfOperation in expressions)
        Position = ulong.MaxValue;

        // TODO: pool if not used by GetOrCreate below
        var descriptorArray = new DwarfAttributeDescriptor[Attributes.Count];

        for (var i = 0; i < Attributes.Count; i++)
        {
            var attr = Attributes[i];
            attr.UpdateAttributeForm(context);
            descriptorArray[i] = new DwarfAttributeDescriptor(attr.Kind, attr.Form);
        }

        var key = new DwarfAbbreviationItemKey(Tag, Children.Count > 0, new DwarfAttributeDescriptors(descriptorArray));
        var item = context.CurrentUnit!.Abbreviation!.GetOrCreate(key);

        Abbrev = item;

        foreach (var children in Children)
        {
            children.UpdateAbbreviationItem(context);
        }
    }

    public override void Write(DwarfWriter writer)
    {
        var startDIEOffset = Position;
        Debug.Assert(Position == startDIEOffset);
        var abbrev = Abbrev;
        if (abbrev is null)
        {
            throw new InvalidOperationException("Abbreviation is not set");
        }

        writer.WriteULEB128(abbrev.Code);

        foreach (var attr in _attributes)
        {
            attr.Write(writer);
        }

        if (abbrev.HasChildren)
        {
            foreach (var child in _children)
            {
                child.Write(writer);
            }
            writer.WriteULEB128(0);
        }

        Debug.Assert(Size == writer.Position - startDIEOffset);
    }
}