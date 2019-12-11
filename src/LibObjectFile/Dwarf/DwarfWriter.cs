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
                    return DwarfAttributeForm.addr;

                case DwarfAttributeEncoding.Block:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is Stream))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a System.IO.Stream");
                    }

                    return DwarfAttributeForm.block;

                case DwarfAttributeEncoding.Constant:
                    
                    if (attr.ValueAsU64 <= byte.MaxValue)
                    {
                        return DwarfAttributeForm.data1;
                    }
                    
                    if (attr.ValueAsU64 <= ushort.MaxValue)
                    {
                        return DwarfAttributeForm.data2;
                    }

                    if (attr.ValueAsU64 <= uint.MaxValue)
                    {
                        return DwarfAttributeForm.data4;
                    }

                    return DwarfAttributeForm.data8;

                case DwarfAttributeEncoding.ExpressionLocation:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is DwarfDIE))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDIE)}");
                    }

                    return DwarfAttributeForm.ref4;

                case DwarfAttributeEncoding.Flag:
                    return attr.ValueAsBoolean ? DwarfAttributeForm.flag_present : DwarfAttributeForm.flag;

                case DwarfAttributeEncoding.LinePointer:
                    VerifyAttributeValueNotNull(attr);

                    if (!(attr.ValueAsObject is DwarfDebugLine))
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The value of attribute {attr} from DIE {attr.Parent} must be a {nameof(DwarfDebugLine)}");
                    }

                    return DwarfAttributeForm.sec_offset;


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

                    return DwarfAttributeForm.strp;

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
                    return DwarfAttributeForm.sec_offset;
            }

            Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The encoding {encoding} of attribute {attr} from DIE {attr.Parent} is not supported.");
            return DwarfAttributeForm.data8;
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
                case DwarfAttributeForm.addr:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUInt(attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.data1:
                    _sizeOf += 1; // WriteU8((byte)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.data2:
                    _sizeOf += 2; // WriteU16((ushort)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.data4:
                    _sizeOf += 4; // WriteU32((uint)attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.data8:
                    _sizeOf += 8; // WriteU64(attr.ValueAsU64);
                    break;
                case DwarfAttributeForm.@string:
                    _sizeOf += SizeOfStringUTF8NullTerminated((string)attr.ValueAsObject);
                    break;
                case DwarfAttributeForm.block:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += DwarfHelper.SizeOfLEB128((ulong)stream.Length);
                    _sizeOf += (ulong) stream.Length;
                    break;
                }
                case DwarfAttributeForm.block1:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 1;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.block2:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 2;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.block4:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    _sizeOf += 4;
                    _sizeOf += (ulong)stream.Length;
                    break;
                }
                case DwarfAttributeForm.flag:
                    _sizeOf += 1; // WriteU8((byte)(attr.ValueAsU64 != 0 ? 1 : 0));
                    break;
                case DwarfAttributeForm.sdata:
                    _sizeOf += DwarfHelper.SizeOfILEB128(attr.ValueAsI64); // WriteILEB128(attr.ValueAsI64);
                    break;
                case DwarfAttributeForm.strp:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUIntFromEncoding(offset);
                    break;
                case DwarfAttributeForm.udata:
                    _sizeOf += DwarfHelper.SizeOfLEB128(attr.ValueAsU64); // ReadULEB128();
                    break;
                case DwarfAttributeForm.ref_addr:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding); // WriteUIntFromEncoding(dieRef.Offset);
                    break;
                case DwarfAttributeForm.ref1:
                    _sizeOf += 1; // WriteU8((byte)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.ref2:
                    _sizeOf += 2; // WriteU16((ushort)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.ref4:
                    _sizeOf += 4; // WriteU32((uint)(dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.ref8:
                    _sizeOf += 8; // WriteU64((dieRef.Offset - _currentUnit.Offset));
                    break;
                case DwarfAttributeForm.ref_udata:
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
                case DwarfAttributeForm.sec_offset:
                    _sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding);
                    break;

                case DwarfAttributeForm.exprloc:
                    UpdateLayoutExpression((DwarfExpression)attr.ValueAsObject);
                    break;

                case DwarfAttributeForm.flag_present:
                    break;

                case DwarfAttributeForm.ref_sig8:
                    _sizeOf += 8; // WriteU64(attr.ValueAsU64);
                    break;

                case DwarfAttributeForm.strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.ref_sup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.strp_sup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.line_strp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.implicit_const: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.ref_sup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNU_addr_index: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNU_str_index: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNU_ref_alt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNU_strp_alt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
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
                case DwarfAttributeForm.addr:
                {
                    WriteUInt(attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.data1:
                {
                    WriteU8((byte)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.data2:
                {
                    WriteU16((ushort)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.data4:
                {
                    WriteU32((uint)attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.data8:
                {
                    WriteU64(attr.ValueAsU64);
                    break;
                }
                case DwarfAttributeForm.@string:
                {
                    WriteStringUTF8NullTerminated((string)attr.ValueAsObject);
                    break;
                }
                case DwarfAttributeForm.block:
                {
                    var stream = (Stream) attr.ValueAsObject;
                    WriteULEB128((ulong)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.block1:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU8((byte)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.block2:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU16((ushort)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.block4:
                {
                    var stream = (Stream)attr.ValueAsObject;
                    WriteU32((uint)stream.Length);
                    Write(stream);
                    break;
                }
                case DwarfAttributeForm.flag:
                {
                    WriteU8((byte) (attr.ValueAsU64 != 0 ? 1 : 0));
                    break;
                }
                case DwarfAttributeForm.sdata:
                {
                    WriteILEB128(attr.ValueAsI64);
                    break;
                }
                case DwarfAttributeForm.strp:
                {
                    var offset = _parent.DebugStringTable.GetOrCreateString((string) attr.ValueAsObject);
                    WriteUIntFromEncoding(offset);
                    break;
                }
                case DwarfAttributeForm.udata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    break;
                }
                case DwarfAttributeForm.ref_addr:
                {
                    var dieRef = (DwarfDIE) attr.ValueAsObject;
                    WriteUIntFromEncoding(dieRef.Offset);
                    break;
                }
                case DwarfAttributeForm.ref1:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU8((byte)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.ref2:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU16((ushort)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.ref4:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU32((uint)(dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.ref8:
                {
                    var dieRef = (DwarfDIE)attr.ValueAsObject;
                    WriteU64((dieRef.Offset - _currentUnit.Offset));
                    break;
                }
                case DwarfAttributeForm.ref_udata:
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
                case DwarfAttributeForm.sec_offset:
                {
                    WriteUIntFromEncoding(attr.ValueAsU64);
                    break;
                }

                case DwarfAttributeForm.exprloc:
                {
                    WriteExpression((DwarfExpression) attr.ValueAsObject);
                    break;
                }

                case DwarfAttributeForm.flag_present:
                {
                    Debug.Assert(attr.ValueAsBoolean);
                    break;
                }

                case DwarfAttributeForm.ref_sig8:
                {
                    WriteU64(attr.ValueAsU64);
                    break;
                }

                case DwarfAttributeForm.strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfAttributeForm.addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfAttributeForm.ref_sup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfAttributeForm.strp_sup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfAttributeForm.data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfAttributeForm.line_strp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfAttributeForm.implicit_const: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfAttributeForm.loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfAttributeForm.rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfAttributeForm.ref_sup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfAttributeForm.strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfAttributeForm.strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfAttributeForm.strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfAttributeForm.strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfAttributeForm.addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfAttributeForm.addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfAttributeForm.addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfAttributeForm.addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfAttributeForm.GNU_addr_index: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfAttributeForm.GNU_str_index: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfAttributeForm.GNU_ref_alt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfAttributeForm.GNU_strp_alt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
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