// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfWriter : DwarfReaderWriter
    {
        private Dictionary<DwarfDIE, DwarfAbbreviationItem> _mapDIEToAbbreviationCode;
        private Queue<DwarfDIE> _diesToWrite;
        private DwarfFile _parent;
        private DwarfUnit _currentUnit;
        private ulong _sizeOf;

        internal DwarfWriter(DwarfWriterContext context, DiagnosticBag diagnostics) : base(context, diagnostics)
        {
        }

        public override bool IsReadOnly => false;
        
        public new DwarfWriterContext Context => (DwarfWriterContext)base.Context;
        
        internal void UpdateLayout(DiagnosticBag diagnostics, DwarfDebugInfoSection debugInfo)
        {
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

        internal void UpdateLayoutUnit(DwarfUnit unit)
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

            unit.UpdateLayoutInternal(this, ref _sizeOf);

            UpdateLayoutDIE(unit.Root);
            // TODO: check size of unit length

            unit.Size = _sizeOf;
        }

        private void UpdateLayoutDIE(DwarfDIE die)
        {
            die.Offset = _sizeOf;

            var abbreviationItem = _mapDIEToAbbreviationCode[die];
            _sizeOf += DwarfHelper.SizeOfLEB128(abbreviationItem.Code); // WriteULEB128(abbreviationItem.Code);

            var descriptors = abbreviationItem.Descriptors;
            for (var i = 0; i < die.Attributes.Count; i++)
            {
                var attr = die.Attributes[i];
                var descriptor = descriptors[i];
                UpdateLayoutAttribute(descriptor.Form, attr);
            }

            foreach (var children in die.Children)
            {
                UpdateLayoutDIE(die);
            }

            die.Size = _sizeOf;
        }


        internal void Write(DwarfDebugInfoSection debugInfo)
        {
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
            var abbreviationItem = _mapDIEToAbbreviationCode[die];
            WriteULEB128(abbreviationItem.Code);

            var descriptors = abbreviationItem.Descriptors;
            for (var i = 0; i < die.Attributes.Count; i++)
            {
                var attr = die.Attributes[i];
                var descriptor = descriptors[i];
                WriteAttribute(descriptor.Form, attr);
            }

            foreach (var children in die.Children)
            {
                WriteDIE(die);
            }
        }

        private DwarfAttributeForm ComputeAttributeForm(DwarfAttribute attr)
        {
            var key = attr.Kind;
            var encoding = DwarfHelper.GetAttributeEncoding(key);

            if (encoding == DwarfAttributeEncoding.None)
            {
                throw new InvalidOperationException($"Unsupported attribute {attr} with unknown encoding");
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

                    if (!(attr.ValueAsObject is DwarfDIE))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDIE)}");
                    }

                    return DwarfAttributeForm.Ref4;

                case DwarfAttributeEncoding.Flag:
                    return attr.ValueAsBoolean ? DwarfAttributeForm.FlagPresent : DwarfAttributeForm.Flag;

                case DwarfAttributeEncoding.LinePointer:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is DwarfDebugLine))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDebugLine)}");
                    }

                    return DwarfAttributeForm.SecOffset;


                case DwarfAttributeEncoding.Reference:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is DwarfDIE))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDIE)}");
                    }

                    return Context.DefaultAttributeFormForReference;

                case DwarfAttributeEncoding.String:
                    VerifyAttributeValueNotNull(attr);

                    if (attr.ValueAsObject is string str)
                    {
                        // Create string offset
                        _parent.DebugStringTable.GetOrCreateString(str);
                    }
                    else
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a string.");
                    }

                    return DwarfAttributeForm.Strp;

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
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUInt(attr.ValueAsU64);
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
                    _sizeOf += DwarfHelper.SizeOfLEB128((ulong)stream.Length);
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
                    _sizeOf += DwarfHelper.SizeOfLEB128(attr.ValueAsU64); // ReadULEB128();
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
                    _sizeOf += DwarfHelper.SizeOfLEB128(dieRef.Offset - _currentUnit.Offset); // WriteULEB128((dieRef.Offset - _currentUnit.Offset));
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
                    UpdateLayoutExpression((DwarfExpression)attr.ValueAsObject);
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

            attr.Size = _sizeOf;
        }

        private void UpdateLayoutExpression(DwarfExpression attrValueAsObject)
        {
            
        }
        
        private void WriteAttribute(DwarfAttributeFormEx form, DwarfAttribute attr)
        {

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
                    var offset = _parent.DebugStringTable.GetOrCreateString((string) attr.ValueAsObject);
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
        }

        private void WriteExpression(DwarfExpression expression)
        {
            WriteULEB128(expression.Size);

            foreach(var op in expression.Operations)
            {
                // TODO
            }
        }
    }
}