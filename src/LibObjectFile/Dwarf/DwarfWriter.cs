// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfWriter : DwarfReaderWriter
    {
        private DwarfFile _parent;
        private DwarfUnit _currentUnit;
        private ulong _sizeOf;
        private DwarfAbbreviation _currentAbbreviation;

        internal DwarfWriter(DwarfWriterContext context, DiagnosticBag diagnostics) : base(context, diagnostics)
        {
        }

        public override bool IsReadOnly => false;
        
        public new DwarfWriterContext Context => (DwarfWriterContext)base.Context;
        
        internal void UpdateLayout(DiagnosticBag diagnostics, DwarfInfoSection debugInfo)
        {
            _parent = debugInfo.Parent;
            var previousDiagnostics = Diagnostics;
            Diagnostics = diagnostics;
            try
            {
                _sizeOf = 0;
                foreach (var unit in debugInfo.Units)
                {
                    _currentUnit = unit;
                    UpdateLayoutUnit(unit);
                }
            }
            finally
            {
                Diagnostics = previousDiagnostics;
            }

        }

        private void UpdateLayoutUnit(DwarfUnit unit)
        {
            unit.Offset = _sizeOf;

            // 1. unit_length 
            Is64BitEncoding = unit.Is64BitEncoding;
            _sizeOf += DwarfHelper.SizeOfUnitLength(Is64BitEncoding);
            // 2. version (uhalf) 
            _sizeOf += sizeof(ushort); // WriteU16(unit.Version);

            if (unit.Version >= 5)
            {
                // 3. unit_type (ubyte)
                _sizeOf += 1; // WriteU8(unit.Kind.Value);

            }

            // Update the layout specific to the Unit instance
            unit.UpdateLayoutInternal(this, ref _sizeOf);

            // Before updating the layout, we need to compute the abbreviation
            ComputeAbbreviation(unit);

            // Compute the full layout of all DIE and attributes (once abbreviation are calculated)
            UpdateLayoutDIE(unit.Root);
            
            unit.Size = _sizeOf - unit.Offset;
            unit.UnitLength = unit.Size - DwarfHelper.SizeOfUnitLength(Is64BitEncoding);
        }

        private void UpdateLayoutDIE(DwarfDIE die)
        {
            die.Offset = _sizeOf;

            var abbrev = die.Abbrev;
            _sizeOf += DwarfHelper.SizeOfULEB128(abbrev.Code); // WriteULEB128(abbreviationItem.Code);

            var descriptors = abbrev.Descriptors;
            for (var i = 0; i < die.Attributes.Count; i++)
            {
                var attr = die.Attributes[i];
                var descriptor = descriptors[i];
                UpdateLayoutAttribute(descriptor.Form, attr);
            }

            if (abbrev.HasChildren)
            {
                foreach (var children in die.Children)
                {
                    UpdateLayoutDIE(children);
                }

                // Encode abbreviation 0 code
                _sizeOf += DwarfHelper.SizeOfULEB128(0);
            }
            
            die.Size = _sizeOf - die.Offset;
        }
        
        internal void Write(DwarfInfoSection debugInfo)
        {
            _parent = debugInfo.Parent;
            foreach (var unit in debugInfo.Units)
            {
                _currentUnit = unit;
                WriteUnit(unit);
            }
        }

        private void WriteUnit(DwarfUnit unit)
        {
            // 1. unit_length 
            Is64BitEncoding = unit.Is64BitEncoding;
            WriteUnitLength(unit.Size);
            // 2. version (uhalf) 
            WriteU16(unit.Version);

            if (unit.Version >= 5)
            {
                // 3. unit_type (ubyte)
                WriteU8((byte)unit.Kind.Value);
            }

            unit.WriteHeaderInternal(this);

            WriteDIE(unit.Root);
            // TODO: check size of unit length
        }

        private void WriteDIE(DwarfDIE die)
        {
            var startDIEOffset = Offset;
            Debug.Assert(die.Offset == startDIEOffset);
            var abbrev = die.Abbrev;
            WriteULEB128(abbrev.Code);

            var descriptors = abbrev.Descriptors;
            for (var i = 0; i < die.Attributes.Count; i++)
            {
                var attr = die.Attributes[i];
                var descriptor = descriptors[i];
                WriteAttribute(descriptor.Form, attr);
            }

            if (abbrev.HasChildren)
            {
                foreach (var children in die.Children)
                {
                    WriteDIE(children);
                }
                WriteULEB128(0);
            }
            Debug.Assert(die.Size == Offset - startDIEOffset);
        }
        
        private void ComputeAbbreviation(DwarfUnit unit)
        {
            // Reset a new abbreviation
            // TODO: Make this configurable
            var abbreviation = new DwarfAbbreviation();
            unit.Abbreviation = abbreviation;
            _parent.AbbreviationTable.AddAbbreviation(abbreviation);

            _currentAbbreviation = unit.Abbreviation;
            ComputeAbbreviationItem(unit.Root);
        }

        private void ComputeAbbreviationItem(DwarfDIE die)
        {
            // Initialize the offset of DIE to ulong.MaxValue to make sure that when we have a reference
            // to it, we can detect if it is a forward or backward reference.
            // If it is a backward reference, we will be able to encode the offset
            // otherwise we will have to pad the encoding with NOP (for DwarfOperation in expressions)
            die.Offset = ulong.MaxValue;

            // TODO: pool if not used by GetOrCreate below
            var descriptorArray = new DwarfAttributeDescriptor[die.Attributes.Count];
            
            for (var i = 0; i < die.Attributes.Count; i++)
            {
                var attr = die.Attributes[i];
                descriptorArray[i] = new DwarfAttributeDescriptor(attr.Kind, ComputeAttributeForm(attr));
            }

            var key = new DwarfAbbreviationItemKey(die.Tag, die.Children.Count > 0, new DwarfAttributeDescriptors(descriptorArray));
            var item = _currentAbbreviation.GetOrCreate(key);

            die.Abbrev = item;

            foreach (var children in die.Children)
            {
                ComputeAbbreviationItem(children);
            }
        }

        private DwarfAttributeForm ComputeAttributeForm(DwarfAttribute attr)
        {
            var key = attr.Kind;
            var encoding = DwarfHelper.GetAttributeEncoding(key);

            if (encoding == DwarfAttributeEncoding.None)
            {
                switch (attr.Kind.Value)
                {
                    case DwarfAttributeKind.GNUAllCallSites:
                    case DwarfAttributeKind.GNUAllTailCallSites:
                        encoding = DwarfAttributeEncoding.Flag;
                        break;
                    case DwarfAttributeKind.GNUCallSiteTarget:
                    case DwarfAttributeKind.GNUCallSiteValue:
                        encoding = DwarfAttributeEncoding.ExpressionLocation | DwarfAttributeEncoding.LocationList;
                        break;

                    default:
                        // TODO: Add pluggable support for requesting attribute encoding here
                        throw new InvalidOperationException($"Unsupported attribute {attr} with unknown encoding");
                }
            }

            // If the attribute has a requested encoding
            if (attr.Encoding.HasValue)
            {
                var requestedEncoding = attr.Encoding.Value;
                if ((encoding & requestedEncoding) == 0)
                {
                    throw new InvalidOperationException($"Requested encoding {requestedEncoding} for {attr} doesn't match supported encoding {encoding} for this attribute");
                }
                // Replace encoding with requested encoding
                encoding = requestedEncoding;
            }

            // Do we still have a complex encoding?
            if ((((uint)encoding) & ((uint)encoding - 1)) != 0U)
            {
                // if we have, try to detect which one we should select
                if (attr.ValueAsObject is DwarfDIE)
                {
                    if ((encoding & DwarfAttributeEncoding.Reference) == 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The DIE value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting Reference.");
                    }
                    encoding = DwarfAttributeEncoding.Reference;
                }
                else if (attr.ValueAsObject is Stream)
                {
                    if ((encoding & DwarfAttributeEncoding.Block) == 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The Stream value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting Block.");
                    }
                    encoding = DwarfAttributeEncoding.Block;
                }
                else if (attr.ValueAsObject is string)
                {
                    if ((encoding & DwarfAttributeEncoding.String) == 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The string value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting String.");
                    }
                    encoding = DwarfAttributeEncoding.String;
                }
                else if (attr.ValueAsObject is DwarfExpression)
                {
                    if ((encoding & DwarfAttributeEncoding.ExpressionLocation) == 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The string value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting ExpressionLocation.");
                    }
                    encoding = DwarfAttributeEncoding.ExpressionLocation;
                }
                else if ((encoding & DwarfAttributeEncoding.Address) != 0)
                {
                    if (attr.ValueAsObject != null)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The {attr.ValueAsObject.GetType()} value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting Address.");
                    }
                    encoding = DwarfAttributeEncoding.Address;
                }
                else if ((encoding & DwarfAttributeEncoding.Constant) != 0)
                {
                    if (attr.ValueAsObject != null)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The {attr.ValueAsObject.GetType()} value of attribute {attr} from DIE {attr.Parent} is not valid for supported attribute encoding {encoding}. Expecting Constant.");
                    }
                    encoding = DwarfAttributeEncoding.Constant;
                }
            }

            switch (encoding)
            {
                case DwarfAttributeEncoding.Address:
                    return DwarfAttributeForm.Addr;

                case DwarfAttributeEncoding.Block:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is Stream))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a System.IO.Stream");
                    }

                    return DwarfAttributeForm.Block;

                case DwarfAttributeEncoding.Constant:
                    
                    if (attr.ValueAsU64 <= byte.MaxValue)
                    {
                        return DwarfAttributeForm.Data1;
                    }
                    
                    if (attr.ValueAsU64 <= ushort.MaxValue)
                    {
                        return DwarfAttributeForm.Data2;
                    }

                    if (attr.ValueAsU64 <= uint.MaxValue)
                    {
                        return DwarfAttributeForm.Data4;
                    }

                    return DwarfAttributeForm.Data8;

                case DwarfAttributeEncoding.ExpressionLocation:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is DwarfExpression))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfExpression)}");
                    }

                    return DwarfAttributeForm.Exprloc;

                case DwarfAttributeEncoding.Flag:
                    return attr.ValueAsBoolean ? DwarfAttributeForm.FlagPresent : DwarfAttributeForm.Flag;

                case DwarfAttributeEncoding.LinePointer:
                    bool canHaveNull = attr.Kind.Value == DwarfAttributeKind.StmtList;
                    if (!canHaveNull)
                    {
                        VerifyAttributeValueNotNull(attr);

                        if (!(attr.ValueAsObject is DwarfLine))
                        {
                            Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfLine)}");
                        }
                    }

                    return DwarfAttributeForm.SecOffset;


                case DwarfAttributeEncoding.Reference:
                    VerifyAttributeValueNotNull(attr);

                    if (attr.ValueAsObject is DwarfDIE die)
                    {
                        var dieParentUnit = die.GetParentUnit();
                        // If we are not from the same unit 
                        if (dieParentUnit != _currentUnit)
                        {
                            return DwarfAttributeForm.RefAddr;
                        }
                    }
                    else
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDIE)}");
                    }

                    return Context.DefaultAttributeFormForReference;

                case DwarfAttributeEncoding.String:
                    VerifyAttributeValueNotNull(attr);

                    if (attr.ValueAsObject is string str)
                    {
                        // Create string offset
                        if (_parent.StringTable.Contains(str))
                        {
                            return DwarfAttributeForm.Strp;
                        }
                    }
                    else
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a string.");
                    }

                    return DwarfAttributeForm.String;

                case DwarfAttributeEncoding.RangeList:
                case DwarfAttributeEncoding.LocationList:
                case DwarfAttributeEncoding.Indirect:
                case DwarfAttributeEncoding.AddressPointer:
                case DwarfAttributeEncoding.LocationListsPointer:
                case DwarfAttributeEncoding.RangeListsPointer:
                case DwarfAttributeEncoding.StringOffsetPointer:
                case DwarfAttributeEncoding.LocationListPointer:
                case DwarfAttributeEncoding.MacroPointer:
                case DwarfAttributeEncoding.RangeListPointer:
                    return DwarfAttributeForm.SecOffset;
            }

            Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The encoding {encoding} of attribute {attr} from DIE {attr.Parent} is not supported.");
            return DwarfAttributeForm.Data8;
        }

        private void VerifyAttributeValueNotNull(DwarfAttribute attr)
        {
            if (attr.ValueAsObject == null)
            {
                Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object value of attribute {attr} from DIE {attr.Parent} cannot be null");
            }
        }

        private void UpdateLayoutAttribute(DwarfAttributeFormEx form, DwarfAttribute attr)
        {
            attr.Offset = _sizeOf;

            switch (form.Value)
            {
                case DwarfAttributeForm.Addr:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitAddress); // WriteUInt(attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.Data1:
                    _sizeOf += 1; // WriteU8((byte)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.Data2:
                    _sizeOf += 2; // WriteU16((ushort)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.Data4:
                    _sizeOf += 4; // WriteU32((uint)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.Data8:
                    _sizeOf += 8; // WriteU64(attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.String:
                    _sizeOf += SizeOfStringUTF8NullTerminated((string)attr.ValueAsObject);
                    break;
                case DwarfAttributeForm.Block:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += DwarfHelper.SizeOfULEB128((ulong)stream.Length);
                    _sizeOf += (ulong) stream.Length;
                    break;
                }
                case DwarfAttributeForm.Block1:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 1;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.Block2:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 2;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.Block4:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 4;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.Flag:
                    _sizeOf += 1; // WriteU8((byte)(attr.ValueAsU64 != 0 ? 1 : 0));
                    break;
                case DwarfAttributeForm.Sdata:
                    _sizeOf += DwarfHelper.SizeOfILEB128(attr.ValueAsI64); // WriteILEB128(attr.ValueAsI64);
                    break;
                case DwarfAttributeForm.Strp:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUIntFromEncoding(offset);
                    break;
                case DwarfAttributeForm.Udata:
                    _sizeOf += DwarfHelper.SizeOfULEB128(attr.ValueAsU64); // ReadULEB128();
                    break;
                case DwarfAttributeForm.RefAddr:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUIntFromEncoding(dieRef.Offset);
                    break;
                case DwarfAttributeForm.Ref1:
                    _sizeOf += 1; // WriteU8((byte)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref2:
                    _sizeOf += 2; // WriteU16((ushort)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref4:
                    _sizeOf += 4; // WriteU32((uint)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.Ref8:
                    _sizeOf += 8; // WriteU64((dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.RefUdata:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    _sizeOf += DwarfHelper.SizeOfULEB128(dieRef.Offset - _currentUnit.Offset); // WriteULEB128((dieRef.Offset - _currentUnit.Offset));
                    break;
                }

                //case DwarfAttributeForm.indirect:
                //{
                //    attributeForm = ReadLEB128As<DwarfAttributeForm>();
                //    goto indirect;
                //}

                // addptr
                // lineptr
                // loclist
                // loclistptr
                // macptr
                // rnglist
                // rngrlistptr
                // stroffsetsptr
                case DwarfAttributeForm.SecOffset:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding);
                    break;

                case DwarfAttributeForm.Exprloc:
                    var expr = (DwarfExpression) attr.ValueAsObject;
                    UpdateLayoutExpression((DwarfExpression)attr.ValueAsObject);
                    var deltaLength = DwarfHelper.SizeOfULEB128(expr.Size);
                    expr.Offset += deltaLength;
                    foreach (var op in expr.InternalOperations)
                    {
                        op.Offset += deltaLength;
                    }
                    _sizeOf += deltaLength;
                    break;

                case DwarfAttributeForm.FlagPresent:
                    break;

                case DwarfAttributeForm.RefSig8:
                    _sizeOf += 8; // WriteU64(attr.ValueAsU64);
                    break;

                case DwarfAttributeForm.Strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.Addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.RefSup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.StrpSup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.Data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.LineStrp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.ImplicitConst: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.Loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.Rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.RefSup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.Strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.Strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.Strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.Strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.Addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.Addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.Addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.Addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNUAddrIndex: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNUStrIndex: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNURefAlt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNUStrpAlt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {form}");
            }

            attr.Size = _sizeOf - attr.Offset;
        }

        private void UpdateLayoutExpression(DwarfExpression expr)
        {
            expr.Offset = _sizeOf;

            foreach (var op in expr.InternalOperations)
            {
                UpdateLayoutOperation(op);
            }

            expr.Size = _sizeOf - expr.Offset;
        }

        private void UpdateLayoutOperation(DwarfOperation op)
        {
            op.Offset = _sizeOf;

            // 1 byte per opcode
            _sizeOf += 1;

            switch (op.Kind.Value)
            {
                case DwarfOperationKind.Addr:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitAddress);
                    break;
                case DwarfOperationKind.Const1u:
                case DwarfOperationKind.Const1s:
                case DwarfOperationKind.Pick:
                case DwarfOperationKind.DerefSize:
                case DwarfOperationKind.XderefSize:
                    _sizeOf += 1;
                    break;
                case DwarfOperationKind.Const2u:
                case DwarfOperationKind.Const2s:
                case DwarfOperationKind.Bra:
                case DwarfOperationKind.Skip:
                case DwarfOperationKind.Call2:
                    _sizeOf += 2;
                    break;
                case DwarfOperationKind.Const4u:
                case DwarfOperationKind.Const4s:
                case DwarfOperationKind.Call4:
                    _sizeOf += 4;
                    break;
                case DwarfOperationKind.Const8u:
                case DwarfOperationKind.Const8s:
                    _sizeOf += 8;
                    break;

                case DwarfOperationKind.Constu:
                case DwarfOperationKind.PlusUconst:
                case DwarfOperationKind.Regx:
                case DwarfOperationKind.Piece:
                case DwarfOperationKind.Addrx:
                case DwarfOperationKind.GNUAddrIndex:
                case DwarfOperationKind.Constx:
                case DwarfOperationKind.GNUConstIndex:
                    _sizeOf += DwarfHelper.SizeOfULEB128(op.Operand1.U64);
                    break;

                case DwarfOperationKind.Consts:
                case DwarfOperationKind.Fbreg:
                    _sizeOf += DwarfHelper.SizeOfILEB128(op.Operand1.I64);
                    break;

                case DwarfOperationKind.Deref:
                case DwarfOperationKind.Dup:
                case DwarfOperationKind.Drop:
                case DwarfOperationKind.Over:
                case DwarfOperationKind.Swap:
                case DwarfOperationKind.Rot:
                case DwarfOperationKind.Xderef:
                case DwarfOperationKind.Abs:
                case DwarfOperationKind.And:
                case DwarfOperationKind.Div:
                case DwarfOperationKind.Minus:
                case DwarfOperationKind.Mod:
                case DwarfOperationKind.Mul:
                case DwarfOperationKind.Neg:
                case DwarfOperationKind.Not:
                case DwarfOperationKind.Or:
                case DwarfOperationKind.Plus:
                case DwarfOperationKind.Shl:
                case DwarfOperationKind.Shr:
                case DwarfOperationKind.Shra:
                case DwarfOperationKind.Xor:
                case DwarfOperationKind.Eq:
                case DwarfOperationKind.Ge:
                case DwarfOperationKind.Gt:
                case DwarfOperationKind.Le:
                case DwarfOperationKind.Lt:
                case DwarfOperationKind.Ne:
                case DwarfOperationKind.Nop:
                case DwarfOperationKind.PushObjectAddress:
                case DwarfOperationKind.FormTlsAddress:
                case DwarfOperationKind.CallFrameCfa:
                case DwarfOperationKind.Lit0:
                case DwarfOperationKind.Lit1:
                case DwarfOperationKind.Lit2:
                case DwarfOperationKind.Lit3:
                case DwarfOperationKind.Lit4:
                case DwarfOperationKind.Lit5:
                case DwarfOperationKind.Lit6:
                case DwarfOperationKind.Lit7:
                case DwarfOperationKind.Lit8:
                case DwarfOperationKind.Lit9:
                case DwarfOperationKind.Lit10:
                case DwarfOperationKind.Lit11:
                case DwarfOperationKind.Lit12:
                case DwarfOperationKind.Lit13:
                case DwarfOperationKind.Lit14:
                case DwarfOperationKind.Lit15:
                case DwarfOperationKind.Lit16:
                case DwarfOperationKind.Lit17:
                case DwarfOperationKind.Lit18:
                case DwarfOperationKind.Lit19:
                case DwarfOperationKind.Lit20:
                case DwarfOperationKind.Lit21:
                case DwarfOperationKind.Lit22:
                case DwarfOperationKind.Lit23:
                case DwarfOperationKind.Lit24:
                case DwarfOperationKind.Lit25:
                case DwarfOperationKind.Lit26:
                case DwarfOperationKind.Lit27:
                case DwarfOperationKind.Lit28:
                case DwarfOperationKind.Lit29:
                case DwarfOperationKind.Lit30:
                case DwarfOperationKind.Lit31:
                case DwarfOperationKind.Reg0:
                case DwarfOperationKind.Reg1:
                case DwarfOperationKind.Reg2:
                case DwarfOperationKind.Reg3:
                case DwarfOperationKind.Reg4:
                case DwarfOperationKind.Reg5:
                case DwarfOperationKind.Reg6:
                case DwarfOperationKind.Reg7:
                case DwarfOperationKind.Reg8:
                case DwarfOperationKind.Reg9:
                case DwarfOperationKind.Reg10:
                case DwarfOperationKind.Reg11:
                case DwarfOperationKind.Reg12:
                case DwarfOperationKind.Reg13:
                case DwarfOperationKind.Reg14:
                case DwarfOperationKind.Reg15:
                case DwarfOperationKind.Reg16:
                case DwarfOperationKind.Reg17:
                case DwarfOperationKind.Reg18:
                case DwarfOperationKind.Reg19:
                case DwarfOperationKind.Reg20:
                case DwarfOperationKind.Reg21:
                case DwarfOperationKind.Reg22:
                case DwarfOperationKind.Reg23:
                case DwarfOperationKind.Reg24:
                case DwarfOperationKind.Reg25:
                case DwarfOperationKind.Reg26:
                case DwarfOperationKind.Reg27:
                case DwarfOperationKind.Reg28:
                case DwarfOperationKind.Reg29:
                case DwarfOperationKind.Reg30:
                case DwarfOperationKind.Reg31:
                case DwarfOperationKind.StackValue:
                case DwarfOperationKind.GNUPushTlsAddress:
                case DwarfOperationKind.GNUUninit:
                    break;

                case DwarfOperationKind.Breg0:
                case DwarfOperationKind.Breg1:
                case DwarfOperationKind.Breg2:
                case DwarfOperationKind.Breg3:
                case DwarfOperationKind.Breg4:
                case DwarfOperationKind.Breg5:
                case DwarfOperationKind.Breg6:
                case DwarfOperationKind.Breg7:
                case DwarfOperationKind.Breg8:
                case DwarfOperationKind.Breg9:
                case DwarfOperationKind.Breg10:
                case DwarfOperationKind.Breg11:
                case DwarfOperationKind.Breg12:
                case DwarfOperationKind.Breg13:
                case DwarfOperationKind.Breg14:
                case DwarfOperationKind.Breg15:
                case DwarfOperationKind.Breg16:
                case DwarfOperationKind.Breg17:
                case DwarfOperationKind.Breg18:
                case DwarfOperationKind.Breg19:
                case DwarfOperationKind.Breg20:
                case DwarfOperationKind.Breg21:
                case DwarfOperationKind.Breg22:
                case DwarfOperationKind.Breg23:
                case DwarfOperationKind.Breg24:
                case DwarfOperationKind.Breg25:
                case DwarfOperationKind.Breg26:
                case DwarfOperationKind.Breg27:
                case DwarfOperationKind.Breg28:
                case DwarfOperationKind.Breg29:
                case DwarfOperationKind.Breg30:
                case DwarfOperationKind.Breg31:
                    _sizeOf += DwarfHelper.SizeOfILEB128(op.Operand2.I64);
                    break;

                case DwarfOperationKind.Bregx:
                    _sizeOf += DwarfHelper.SizeOfULEB128(op.Operand1.U64);
                    _sizeOf += DwarfHelper.SizeOfILEB128(op.Operand2.I64);
                    break;

                case DwarfOperationKind.CallRef:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitAddress);
                    break;

                case DwarfOperationKind.BitPiece:
                    _sizeOf += DwarfHelper.SizeOfULEB128(op.Operand1.U64);
                    _sizeOf += DwarfHelper.SizeOfULEB128(op.Operand2.U64);
                    break;

                case DwarfOperationKind.ImplicitValue:
                    if (op.Operand0 == null)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of implicit value operation {op} from DIE cannot be null.");
                    }
                    else if (op.Operand0 is Stream stream)
                    {
                        var streamSize = (ulong) stream.Length;
                        _sizeOf += DwarfHelper.SizeOfULEB128(streamSize);
                        _sizeOf += streamSize;
                    }
                    else
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of implicit value operation {op} must be a System.IO.Stream.");
                    }

                    break;

                case DwarfOperationKind.ImplicitPointer:
                case DwarfOperationKind.GNUImplicitPointer:
                    //  a reference to a debugging information entry that describes the dereferenced object’s value
                    if (_currentUnit.Version == 2)
                    {
                        _sizeOf += DwarfHelper.SizeOfUInt(Is64BitAddress);
                    }
                    else
                    {
                        _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding);
                    }

                    //  a signed number that is treated as a byte offset from the start of that value
                    _sizeOf += DwarfHelper.SizeOfILEB128(op.Operand1.I64);
                    break;

                case DwarfOperationKind.EntryValue:
                case DwarfOperationKind.GNUEntryValue:
                    if (op.Operand0 == null)
                    {
                        _sizeOf += DwarfHelper.SizeOfULEB128(0);
                    }
                    else if (op.Operand0 is DwarfExpression expr)
                    {
                        UpdateLayoutExpression(expr);
                        _sizeOf += DwarfHelper.SizeOfULEB128(expr.Size);
                    }
                    else
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of EntryValue operation {op} must be a {nameof(DwarfExpression)} instead of {op.Operand0.GetType()}.");
                    }
                    break;

                case DwarfOperationKind.ConstType:
                case DwarfOperationKind.GNUConstType:
                    {
                        // The DW_OP_const_type operation takes three operands

                        // The first operand is an unsigned LEB128 integer that represents the offset
                        // of a debugging information entry in the current compilation unit, which
                        // must be a DW_TAG_base_type entry that provides the type of the constant provided

                        _sizeOf += SizeOfDIEReference(op);
                        _sizeOf += SizeOfEncodedValue(op.Kind, op.Operand1.U64, (byte)op.Operand2.U64);
                        break;
                    }

                case DwarfOperationKind.RegvalType:
                case DwarfOperationKind.GNURegvalType:
                    {
                        // The DW_OP_regval_type operation provides the contents of a given register
                        // interpreted as a value of a given type

                        // The first operand is an unsigned LEB128 number, which identifies a register
                        // whose contents is to be pushed onto the stack
                        _sizeOf += DwarfHelper.SizeOfULEB128(op.Operand1.U64);

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        _sizeOf += SizeOfDIEReference(op);
                        break;
                    }

                case DwarfOperationKind.DerefType:
                case DwarfOperationKind.GNUDerefType:
                case DwarfOperationKind.XderefType:
                    {
                        // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                        // it pops the top stack entry and treats it as an address.

                        // This operand is a 1-byte unsigned integral constant whose value which is the
                        // same as the size of the base type referenced by the second operand
                        _sizeOf += 1;

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        _sizeOf += SizeOfDIEReference(op);
                        break;
                    }

                case DwarfOperationKind.Convert:
                case DwarfOperationKind.GNUConvert:
                case DwarfOperationKind.Reinterpret:
                case DwarfOperationKind.GNUReinterpret:
                    _sizeOf += SizeOfDIEReference(op);
                    break;

                case DwarfOperationKind.GNUEncodedAddr:
                    _sizeOf += SizeOfEncodedValue(op.Kind, op.Operand1.U64, (byte)op.Operand2.U64);
                    break;

                case DwarfOperationKind.GNUParameterRef:
                    _sizeOf += 4;
                    break;

                default:
                    throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {op.Kind} is not supported");
            }

            op.Size = _sizeOf - op.Offset;
        }

        private ulong SizeOfDIEReference(DwarfOperation op)
        {
            if (op.Operand0 == null)
            {
                return DwarfHelper.SizeOfULEB128(0);
            }
            else if (op.Operand0 is DwarfDIE die)
            {
                // TODO: check that die reference is within this section

                if (die.Offset < op.Offset)
                {
                    return DwarfHelper.SizeOfULEB128(die.Offset);
                }
                else
                {
                    // TODO: encode depending on Context.DefaultAttributeFormForReference
                    return DwarfHelper.SizeOfILEB128(uint.MaxValue);
                }
            }
            else
            {
                Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The object operand of {op.Kind} operation {op} must be a {nameof(DwarfDIE)} instead of {op.Operand0.GetType()}.");
            }

            return 0U;
        }
        
        private ulong SizeOfEncodedValue(DwarfOperationKindEx kind, ulong value, byte size)
        {
            switch (size)
            {
                case 0:
                    return 1 + DwarfHelper.SizeOfUInt(Is64BitAddress);
                case 1:
                    return 1 + 1;
                case 2:
                    return 1 + 2;
                case 4:
                    return 1 + 4;
                case 8:
                    return 1 + 8;
                default:
                    // TODO: report via diagnostics in Verify
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        private void WriteAttribute(DwarfAttributeFormEx form, DwarfAttribute attr)
        {
            var startAttributeOffset = Offset;
            Debug.Assert(attr.Offset == startAttributeOffset);
            
            switch (form.Value)
            {
                case DwarfAttributeForm.Addr:
                {
                    WriteUInt(attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data1:
                {
                    WriteU8((byte)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data2:
                {
                    WriteU16((ushort)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data4:
                {
                    WriteU32((uint)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.Data8:
                {
                    WriteU64(attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.String:
                {
                    WriteStringUTF8NullTerminated((string)attr.ValueAsObject);
                    break;
                }
                case DwarfAttributeForm.Block:
                {
                    var stream = (Stream) attr.ValueAsObject;
                    WriteULEB128((ulong)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block1:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU8((byte)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block2:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU16((ushort)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.Block4:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU32((uint)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.Flag:
                {
                    WriteU8((byte) (attr.ValueAsU64 != 0 ? 1 : 0));
                    break;
                }
                case DwarfAttributeForm.Sdata:
                {
                    WriteILEB128(attr.ValueAsI64);
                    break;
                }
                case DwarfAttributeForm.Strp:
                {
                    var offset = _parent.StringTable.GetOrCreateString((string) attr.ValueAsObject);
                    WriteUIntFromEncoding(offset);
                    break;
                }
                case DwarfAttributeForm.Udata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    break;
                }
                case DwarfAttributeForm.RefAddr:
                {
                    var dieRef = (DwarfDIE) attr.ValueAsObject;
                    WriteUIntFromEncoding(dieRef.Offset);
                    break;
                }
                case DwarfAttributeForm.Ref1:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU8((byte)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref2:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU16((ushort)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref4:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU32((uint)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.Ref8:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU64((dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.RefUdata:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteULEB128((dieRef.Offset - _currentUnit.Offset));
                    break;
                }

                //case DwarfAttributeForm.indirect:
                //{
                //    attributeForm = ReadLEB128As<DwarfAttributeForm>();
                //    goto indirect;
                //}

                // addptr
                // lineptr
                // loclist
                // loclistptr
                // macptr
                // rnglist
                // rngrlistptr
                // stroffsetsptr
                case DwarfAttributeForm.SecOffset:
                {
                    WriteUIntFromEncoding(attr.ValueAsU64);
                    break;
                }

                case DwarfAttributeForm.Exprloc:
                {
                    WriteExpression((DwarfExpression) attr.ValueAsObject);
                    break;
                }

                case DwarfAttributeForm.FlagPresent:
                {
                    Debug.Assert(attr.ValueAsBoolean);
                    break;
                }

                case DwarfAttributeForm.RefSig8:
                {
                    WriteU64(attr.ValueAsU64);
                    break;
                }

                case DwarfAttributeForm.Strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.Addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.RefSup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.StrpSup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.Data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.LineStrp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.ImplicitConst: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.Loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.Rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.RefSup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.Strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.Strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.Strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.Strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.Addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.Addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.Addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.Addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNUAddrIndex: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNUStrIndex: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNURefAlt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNUStrpAlt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {form}");
            }

            Debug.Assert(Offset - startAttributeOffset == attr.Size);
        }

        private void WriteExpression(DwarfExpression expression)
        {
            WriteULEB128(expression.Size);
            
            var startExpressionOffset = Offset;
            Debug.Assert(startExpressionOffset == expression.Offset);
            
            foreach (var op in expression.Operations)
            {
                WriteOperation(op);
            }

            Debug.Assert(Offset - startExpressionOffset == expression.Size);
        }

        private void WriteOperation(DwarfOperation op)
        {
            var startOpOffset = Offset;
            Debug.Assert(startOpOffset == op.Offset);

            WriteU8((byte)op.Kind);
            
            switch (op.Kind.Value)
            {
                case DwarfOperationKind.Addr:
                    WriteUInt(op.Operand1.U64);
                    break;
                case DwarfOperationKind.Const1u:
                case DwarfOperationKind.Const1s:
                case DwarfOperationKind.Pick:
                case DwarfOperationKind.DerefSize:
                case DwarfOperationKind.XderefSize:
                    WriteU8((byte)op.Operand1.U64);
                    break;

                case DwarfOperationKind.Const2u:
                case DwarfOperationKind.Const2s:
                    WriteU16((ushort)op.Operand1.U64);
                    break;

                case DwarfOperationKind.Const4u:
                case DwarfOperationKind.Const4s:
                    WriteU32((uint)op.Operand1.U64);
                    break;

                case DwarfOperationKind.Const8u:
                case DwarfOperationKind.Const8s:
                    WriteU64(op.Operand1.U64);
                    break;

                case DwarfOperationKind.Constu:
                case DwarfOperationKind.PlusUconst:
                case DwarfOperationKind.Regx:
                case DwarfOperationKind.Piece:
                case DwarfOperationKind.Addrx:
                case DwarfOperationKind.GNUAddrIndex:
                case DwarfOperationKind.Constx:
                case DwarfOperationKind.GNUConstIndex:
                    WriteULEB128(op.Operand1.U64);
                    break;

                case DwarfOperationKind.Consts:
                case DwarfOperationKind.Fbreg:
                    WriteILEB128(op.Operand1.I64);
                    break;

                case DwarfOperationKind.Deref:
                case DwarfOperationKind.Dup:
                case DwarfOperationKind.Drop:
                case DwarfOperationKind.Over:
                case DwarfOperationKind.Swap:
                case DwarfOperationKind.Rot:
                case DwarfOperationKind.Xderef:
                case DwarfOperationKind.Abs:
                case DwarfOperationKind.And:
                case DwarfOperationKind.Div:
                case DwarfOperationKind.Minus:
                case DwarfOperationKind.Mod:
                case DwarfOperationKind.Mul:
                case DwarfOperationKind.Neg:
                case DwarfOperationKind.Not:
                case DwarfOperationKind.Or:
                case DwarfOperationKind.Plus:
                case DwarfOperationKind.Shl:
                case DwarfOperationKind.Shr:
                case DwarfOperationKind.Shra:
                case DwarfOperationKind.Xor:
                case DwarfOperationKind.Eq:
                case DwarfOperationKind.Ge:
                case DwarfOperationKind.Gt:
                case DwarfOperationKind.Le:
                case DwarfOperationKind.Lt:
                case DwarfOperationKind.Ne:
                case DwarfOperationKind.Nop:
                case DwarfOperationKind.PushObjectAddress:
                case DwarfOperationKind.FormTlsAddress:
                case DwarfOperationKind.CallFrameCfa:
                    break;

                case DwarfOperationKind.Bra:
                case DwarfOperationKind.Skip:
                    WriteU16((ushort)((long)Offset + 2 - (long)((DwarfOperation) op.Operand0).Offset));
                    break;

                case DwarfOperationKind.Lit0:
                case DwarfOperationKind.Lit1:
                case DwarfOperationKind.Lit2:
                case DwarfOperationKind.Lit3:
                case DwarfOperationKind.Lit4:
                case DwarfOperationKind.Lit5:
                case DwarfOperationKind.Lit6:
                case DwarfOperationKind.Lit7:
                case DwarfOperationKind.Lit8:
                case DwarfOperationKind.Lit9:
                case DwarfOperationKind.Lit10:
                case DwarfOperationKind.Lit11:
                case DwarfOperationKind.Lit12:
                case DwarfOperationKind.Lit13:
                case DwarfOperationKind.Lit14:
                case DwarfOperationKind.Lit15:
                case DwarfOperationKind.Lit16:
                case DwarfOperationKind.Lit17:
                case DwarfOperationKind.Lit18:
                case DwarfOperationKind.Lit19:
                case DwarfOperationKind.Lit20:
                case DwarfOperationKind.Lit21:
                case DwarfOperationKind.Lit22:
                case DwarfOperationKind.Lit23:
                case DwarfOperationKind.Lit24:
                case DwarfOperationKind.Lit25:
                case DwarfOperationKind.Lit26:
                case DwarfOperationKind.Lit27:
                case DwarfOperationKind.Lit28:
                case DwarfOperationKind.Lit29:
                case DwarfOperationKind.Lit30:
                case DwarfOperationKind.Lit31:
                case DwarfOperationKind.Reg0:
                case DwarfOperationKind.Reg1:
                case DwarfOperationKind.Reg2:
                case DwarfOperationKind.Reg3:
                case DwarfOperationKind.Reg4:
                case DwarfOperationKind.Reg5:
                case DwarfOperationKind.Reg6:
                case DwarfOperationKind.Reg7:
                case DwarfOperationKind.Reg8:
                case DwarfOperationKind.Reg9:
                case DwarfOperationKind.Reg10:
                case DwarfOperationKind.Reg11:
                case DwarfOperationKind.Reg12:
                case DwarfOperationKind.Reg13:
                case DwarfOperationKind.Reg14:
                case DwarfOperationKind.Reg15:
                case DwarfOperationKind.Reg16:
                case DwarfOperationKind.Reg17:
                case DwarfOperationKind.Reg18:
                case DwarfOperationKind.Reg19:
                case DwarfOperationKind.Reg20:
                case DwarfOperationKind.Reg21:
                case DwarfOperationKind.Reg22:
                case DwarfOperationKind.Reg23:
                case DwarfOperationKind.Reg24:
                case DwarfOperationKind.Reg25:
                case DwarfOperationKind.Reg26:
                case DwarfOperationKind.Reg27:
                case DwarfOperationKind.Reg28:
                case DwarfOperationKind.Reg29:
                case DwarfOperationKind.Reg30:
                case DwarfOperationKind.Reg31:
                case DwarfOperationKind.StackValue:
                    break;

                case DwarfOperationKind.Breg0:
                case DwarfOperationKind.Breg1:
                case DwarfOperationKind.Breg2:
                case DwarfOperationKind.Breg3:
                case DwarfOperationKind.Breg4:
                case DwarfOperationKind.Breg5:
                case DwarfOperationKind.Breg6:
                case DwarfOperationKind.Breg7:
                case DwarfOperationKind.Breg8:
                case DwarfOperationKind.Breg9:
                case DwarfOperationKind.Breg10:
                case DwarfOperationKind.Breg11:
                case DwarfOperationKind.Breg12:
                case DwarfOperationKind.Breg13:
                case DwarfOperationKind.Breg14:
                case DwarfOperationKind.Breg15:
                case DwarfOperationKind.Breg16:
                case DwarfOperationKind.Breg17:
                case DwarfOperationKind.Breg18:
                case DwarfOperationKind.Breg19:
                case DwarfOperationKind.Breg20:
                case DwarfOperationKind.Breg21:
                case DwarfOperationKind.Breg22:
                case DwarfOperationKind.Breg23:
                case DwarfOperationKind.Breg24:
                case DwarfOperationKind.Breg25:
                case DwarfOperationKind.Breg26:
                case DwarfOperationKind.Breg27:
                case DwarfOperationKind.Breg28:
                case DwarfOperationKind.Breg29:
                case DwarfOperationKind.Breg30:
                case DwarfOperationKind.Breg31:
                    WriteILEB128(op.Operand2.I64);
                    break;

                case DwarfOperationKind.Bregx:
                    WriteULEB128(op.Operand1.U64);
                    WriteILEB128(op.Operand2.I64);
                    break;

                case DwarfOperationKind.Call2:
                    WriteU16((ushort)((DwarfDIE)op.Operand0).Offset);
                    break;

                case DwarfOperationKind.Call4:
                    WriteU32((uint)((DwarfDIE)op.Operand0).Offset);
                    break;

                case DwarfOperationKind.CallRef:
                    WriteUInt(((DwarfDIE)op.Operand0).Offset);
                    break;

                case DwarfOperationKind.BitPiece:
                    WriteULEB128(op.Operand1.U64);
                    WriteULEB128(op.Operand2.U64);
                    break;

                case DwarfOperationKind.ImplicitValue:
                {
                    var stream = (Stream) op.Operand0;
                    WriteULEB128((ulong)stream.Position);
                    Write(stream);
                    break;
                }

                case DwarfOperationKind.ImplicitPointer:
                case DwarfOperationKind.GNUImplicitPointer:
                {
                    ulong offset = ((DwarfDIE) op.Operand0).Offset;
                    //  a reference to a debugging information entry that describes the dereferenced object’s value
                    if (_currentUnit.Version == 2)
                    {
                        WriteUInt(offset);
                    }
                    else
                    {
                        WriteUIntFromEncoding(offset);
                    }
                    //  a signed number that is treated as a byte offset from the start of that value
                    WriteILEB128(op.Operand1.I64);
                    break;
                }

                case DwarfOperationKind.EntryValue:
                case DwarfOperationKind.GNUEntryValue:
                {
                    var expression = (DwarfExpression) op.Operand0;
                    WriteULEB128(expression.Size);
                    WriteExpression(expression);
                    break;
                }

                case DwarfOperationKind.ConstType:
                case DwarfOperationKind.GNUConstType:
                {
                    // The DW_OP_const_type operation takes three operands

                    // The first operand is an unsigned LEB128 integer that represents the offset
                    // of a debugging information entry in the current compilation unit, which
                    // must be a DW_TAG_base_type entry that provides the type of the constant provided
                    WriteULEB128(((DwarfDIE)op.Operand0).Offset);
                    WriteEncodedValue(op.Kind, op.Operand1.U64, (byte)op.Operand2.U64);
                    break;
                }

                case DwarfOperationKind.RegvalType:
                case DwarfOperationKind.GNURegvalType:
                {
                    // The DW_OP_regval_type operation provides the contents of a given register
                    // interpreted as a value of a given type

                    // The first operand is an unsigned LEB128 number, which identifies a register
                    // whose contents is to be pushed onto the stack
                    WriteULEB128(op.Operand1.U64);

                    // The second operand is an unsigned LEB128 number that represents the offset
                    // of a debugging information entry in the current compilation unit
                    WriteULEB128(((DwarfDIE)op.Operand0).Offset);
                    break;
                }

                case DwarfOperationKind.DerefType:
                case DwarfOperationKind.GNUDerefType:
                case DwarfOperationKind.XderefType:
                {
                    // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                    // it pops the top stack entry and treats it as an address.

                    // This operand is a 1-byte unsigned integral constant whose value which is the
                    // same as the size of the base type referenced by the second operand
                    WriteU8((byte)op.Operand1.U64);

                    // The second operand is an unsigned LEB128 number that represents the offset
                    // of a debugging information entry in the current compilation unit
                    WriteULEB128(((DwarfDIE)op.Operand0).Offset);
                    break;
                }

                case DwarfOperationKind.Convert:
                case DwarfOperationKind.GNUConvert:
                case DwarfOperationKind.Reinterpret:
                case DwarfOperationKind.GNUReinterpret:
                    WriteULEB128(((DwarfDIE)op.Operand0).Offset);
                    break;

                case DwarfOperationKind.GNUPushTlsAddress:
                case DwarfOperationKind.GNUUninit:
                    break;

                case DwarfOperationKind.GNUEncodedAddr:
                    WriteEncodedValue(op.Kind, op.Operand1.U64, (byte)op.Operand2.U64);
                    break;

                case DwarfOperationKind.GNUParameterRef:
                    WriteU32((uint)op.Operand1.U64);
                    break;

                default:
                    throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {op.Kind} is not supported");
            }

            Debug.Assert(Offset - startOpOffset == op.Size);
        }

        private void WriteEncodedValue(DwarfOperationKindEx kind, ulong value, byte size)
        {
            WriteU8(size);
            switch (size)
            {
                case 0:
                    WriteUInt(value);
                    break;
                case 1:
                    WriteU8((byte)value);
                    break;
                case 2:
                    WriteU16((ushort)value);
                    break;
                case 4:
                    WriteU32((uint) value);
                    break;
                case 8:
                    WriteU64(value);
                    break;
                default:
                    // TODO: report via diagnostics in Verify
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }
    }
}