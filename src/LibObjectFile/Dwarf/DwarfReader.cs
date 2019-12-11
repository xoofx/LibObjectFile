// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfReader : DwarfReaderWriter
    {
        private readonly Dictionary<ulong, DwarfAbbreviation> _abbreviations;
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerCompilationUnit;
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerSection;
        private readonly List<DwarfDIEReference> _unresolvedDIECompilationUnitReference;
        private readonly List<DwarfDIEReference> _attributesWithUnresolvedDIESectionReference;
        private readonly Dictionary<ulong, DwarfDebugLine> _offsetToDebugLine;
        private DiagnosticBag _diagnostics;
        private bool _is64Address;
        private ushort _version;
        private ulong _offsetOfCompilationUnitInSection;
        private DwarfFile _parent;

        internal DwarfReader(DwarfReaderContext context, DiagnosticBag diagnostics) : base(context, diagnostics)
        {
            IsReadOnly = context.IsInputReadOnly;
            _abbreviations = new Dictionary<ulong, DwarfAbbreviation>();
            _registeredDIEPerCompilationUnit = new Dictionary<ulong, DwarfDIE>();
            _registeredDIEPerSection = new Dictionary<ulong, DwarfDIE>();
            _unresolvedDIECompilationUnitReference = new List<DwarfDIEReference>();
            _attributesWithUnresolvedDIESectionReference = new List<DwarfDIEReference>();
            _offsetToDebugLine = new Dictionary<ulong, DwarfDebugLine>();
        }

        public override bool IsReadOnly { get; }

        public new DwarfReaderContext Context => (DwarfReaderContext)base.Context;
        
        internal void Read(DwarfDebugInfoSection debugInfo, DwarfUnitKind defaultUnitKind)
        {
            _diagnostics = Diagnostics;
            _parent = debugInfo.Parent;

            // Prebuild map offset to debug line
            if (_parent.DebugLineSection != null)
            {
                foreach(var debugLine in _parent.DebugLineSection.Lines)
                {
                    _offsetToDebugLine.Add(debugLine.Offset, debugLine);
                }
            }
            
            while (true)
            {
                if (Offset >= Length)
                {
                    break;
                }

                // 7.5 Format of Debugging Information
                // - Each such contribution consists of a compilation unit header

                var startOffset = Offset;
                if (!TryReadUnitHeader(defaultUnitKind, out var cu, out var offsetEndOfUnit))
                {
                    Offset = offsetEndOfUnit;
                    continue;
                }

                _version = cu.Version;
                _is64Address = cu.Is64BitAddress;

                _offsetOfCompilationUnitInSection = startOffset;
                _registeredDIEPerCompilationUnit.Clear();
                _unresolvedDIECompilationUnitReference.Clear();
                
                var abbreviation = ReadAbbreviation(Context.DebugAbbrevStream, cu.DebugAbbreviationOffset);
                
                // Each debugging information entry begins with an unsigned LEB128 number containing the abbreviation code for the entry.
                cu.Abbreviation = abbreviation;
                cu.Root = ReadDIE(abbreviation, 0);

                // Resolve attribute reference within the CU
                foreach (var unresolvedAttrRef in _unresolvedDIECompilationUnitReference)
                {
                    ResolveAttributeReferenceWithinCompilationUnit(unresolvedAttrRef, true);
                }

                debugInfo.AddUnit(cu);
            }

            // Resolve attribute reference within the section
            foreach (var unresolvedAttrRef in _attributesWithUnresolvedDIESectionReference)
            {
                ResolveAttributeReferenceWithinSection(unresolvedAttrRef, true);
            }
        }

        private DwarfAbbreviation ReadAbbreviation(Stream stream, ulong abbreviationOffset)
        {
            if (_abbreviations.TryGetValue(abbreviationOffset, out var abbreviation))
            {
                return abbreviation;
            }

            abbreviation = DwarfAbbreviation.Read(stream, abbreviationOffset);
            _parent.DebugAbbrevTable.AddAbbreviation(abbreviation);

            _abbreviations[abbreviationOffset] = abbreviation;
            return abbreviation;
        }
        
        private DwarfDIE ReadDIE(DwarfAbbreviation abbreviation, int level)
        {
            var startDIEOffset = Offset;
            var abbreviationCode = ReadULEB128();

            if (abbreviationCode == 0)
            {
                return null;
            }

            if (!abbreviation.TryFindByCode(abbreviationCode, out var abbreviationItem))
            {
                throw new InvalidOperationException($"Invalid abbreviation code {abbreviationCode}");
            }
            
            var die = DIEHelper.ConvertTagToDwarfDIE((ushort) abbreviationItem.Tag);
            
            die.Offset = startDIEOffset ;
            die.Tag = abbreviationItem.Tag;

            // Store map offset to DIE to resolve references
            _registeredDIEPerCompilationUnit.Add(startDIEOffset - _offsetOfCompilationUnitInSection, die);
            _registeredDIEPerSection.Add(startDIEOffset, die);

            // Console.WriteLine($" <{level}><{die.Offset:x}> Abbrev Number: {abbreviationCode} ({die.Tag})");

            if (abbreviationItem.Descriptors.Length > 0)
            {
                foreach (var descriptor in abbreviationItem.Descriptors)
                {
                    var attribute = new DwarfAttribute()
                    {
                        Offset = Offset,
                        Kind = descriptor.Kind,
                    };
                    var form = descriptor.Form;
                    ReadAttributeFormRawValue(form, attribute);

                    attribute.Size = Offset - attribute.Offset;

                    ResolveAttributeValue(attribute);
                    
                    die.AddAttribute(attribute);
                }
            }

            if (abbreviationItem.HasChildren)
            {
                while (true)
                {
                    var child = ReadDIE(abbreviation, level+1);
                    if (child == null) break;
                    die.AddChild(child);
                }
            }

            die.Size = Offset - startDIEOffset;

            return die;
        }

        private void ResolveAttributeValue(DwarfAttribute attr)
        {
            switch (attr.Kind.Value)
            {
                case DwarfAttributeKind.decl_file:
                {
                    var file = _parent.DebugLineSection.FileNames[attr.ValueAsI32 - 1];
                    attr.ValueAsU64 = 0;
                    attr.ValueAsObject = file;
                    break;
                }

                case DwarfAttributeKind.stmt_list:
                {
                    if (attr.ValueAsU64 == 0) return;

                    if (_parent.DebugLineSection != null)
                    {
                        if (_offsetToDebugLine.TryGetValue(attr.ValueAsU64, out var debugLine))
                        {
                            attr.ValueAsObject = debugLine;
                        }
                        else
                        {
                            // Log and error
                        }
                    }
                    else
                    {

                        // Log an error
                    }

                    break;

                }
            }
        }
        
        private bool TryReadUnitHeader(DwarfUnitKindEx unitKind,  out DwarfUnit unit, out ulong offsetEndOfUnit)
        {
            var startOffset = Offset;

            unit = null;

            // 1. unit_length 
            var unit_length = ReadUnitLength();

            offsetEndOfUnit = (ulong)Offset + unit_length;

            // 2. version (uhalf) 
            var version = ReadU16();

            if (version <= 2 || version > 5)
            {
                Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {version} is not supported");
                return false;
            }
            
            if (version >= 5)
            {
                // 3. unit_type (ubyte)
                unitKind = new DwarfUnitKindEx(ReadU8());
            }
            
            switch (unitKind.Value)
            {
                case DwarfUnitKind.compile:
                case DwarfUnitKind.partial:
                    unit = new DwarfCompilationUnit();
                    break;

                default:
                    Diagnostics.Error(DiagnosticId.DWARF_ERR_UnsupportedUnitType, $"Unit Type {unitKind} is not supported");
                    return false;
            }

            unit.Kind = unitKind;

            unit.Is64BitEncoding = Is64BitEncoding;
            unit.Offset = startOffset;
            unit.Version = version;

            return unit.TryReadHeaderInternal(this);
        }

        private void ReadAttributeFormRawValue(DwarfAttributeFormEx attributeForm, DwarfAttribute attr)
        {
            indirect:
            switch (attributeForm.Value)
            {
                case DwarfAttributeForm.addr:
                {
                    attr.ValueAsU64 = _is64Address ? ReadU64() : ReadU32();
                    break;
                }

                case DwarfAttributeForm.data1:
                {
                    attr.ValueAsU64 = ReadU8();
                    break;
                }
                case DwarfAttributeForm.data2:
                {
                    attr.ValueAsU64 = ReadU16();
                    break;
                }
                case DwarfAttributeForm.data4:
                {
                    attr.ValueAsU64 = ReadU32();
                    break;
                }
                case DwarfAttributeForm.data8:
                {
                    attr.ValueAsU64 = ReadU64();
                    break;
                }

                case DwarfAttributeForm.@string:
                {
                    attr.ValueAsObject = ReadStringUTF8NullTerminated();
                    break;
                }

                case DwarfAttributeForm.block:
                {
                    var length = ReadULEB128();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.block1:
                {
                    var length = ReadU8();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.block2:
                {
                    var length = ReadU16();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.block4:
                {
                    var length = ReadU32();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }

                case DwarfAttributeForm.flag:
                {
                    attr.ValueAsBoolean = ReadU8() != 0;
                    break;
                }
                case DwarfAttributeForm.sdata:
                {
                    attr.ValueAsI64 = ReadILEB128();
                    break;
                }
                case DwarfAttributeForm.strp:
                {
                    var offset = ReadUIntFromEncoding();
                    if (_parent.DebugStringTable == null)
                    {
                        attr.ValueAsU64 = offset;
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_MissingStringTable, $"The .debug_str {nameof(DwarfFile.DebugStringTable)} is null while a DW_FORM_strp for attribute {attr.Kind} is requesting an access to it");
                    }
                    else
                    {
                        attr.ValueAsObject = _parent.DebugStringTable.GetStringFromOffset(offset);
                    }

                    break;
                }
                case DwarfAttributeForm.udata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    break;
                }
                case DwarfAttributeForm.ref_addr:
                {
                    attr.ValueAsU64 = ReadUIntFromEncoding();
                    ResolveAttributeReferenceWithinSection(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.ref1:
                {
                    attr.ValueAsU64 = ReadU8();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.ref2:
                {
                    attr.ValueAsU64 = ReadU16();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.ref4:
                {
                    attr.ValueAsU64 = ReadU32();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.ref8:
                {
                    attr.ValueAsU64 = ReadU64();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.ref_udata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.indirect:
                {
                    attributeForm = new DwarfAttributeFormEx(this.ReadLEB128AsU32());
                    goto indirect;
                }

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
                    attr.ValueAsU64 = ReadUIntFromEncoding();
                    //Console.WriteLine($"attribute {attr.Key} offset: {attr.ValueAsU64}");
                    break;
                }

                case DwarfAttributeForm.exprloc:
                {
                    var length = ReadULEB128();
                    attr.ValueAsObject = ReadExpression(length);
                    break;
                }

                case DwarfAttributeForm.flag_present:
                {
                    attr.ValueAsBoolean = true;
                    break;
                }

                case DwarfAttributeForm.ref_sig8:
                {
                    var offset = ReadU64();
                    attr.ValueAsU64 = offset;
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
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {attributeForm.Value}");
            }
        }

        private DwarfExpression ReadExpression(ulong size)
        {

            var stream = Stream;
            var startPosition = (ulong)stream.Position;
            var exprLoc = new DwarfExpression {Offset = startPosition, Size = size};
            var endPosition = startPosition + size;

            while ((ulong)stream.Position < endPosition)
            {
                var kind = new DwarfOperationKindEx(stream.ReadU8());
                var op = new DwarfOperation
                {
                    Offset = Offset,
                    Kind = kind
                };
                exprLoc.AddOperation(op);

                switch (kind.Value)
                {
                    case DwarfOperationKind.addr:
                        op.Operand1.U64 = ReadUInt();
                        break;
                    case DwarfOperationKind.const1u:
                        op.Operand1.U64 = ReadU8();
                        break;
                    case DwarfOperationKind.const1s:
                        op.Operand1.I64 = ReadI8();
                        break;
                    case DwarfOperationKind.const2u:
                        op.Operand1.U64 = ReadU16();
                        break;
                    case DwarfOperationKind.const2s:
                        op.Operand1.I64 = ReadI16();
                        break;

                    case DwarfOperationKind.const4u:
                        op.Operand1.U64 = ReadU32();
                        break;
                    case DwarfOperationKind.const4s:
                        op.Operand1.I64 = ReadU32();
                        break;

                    case DwarfOperationKind.const8u:
                        op.Operand1.U64 = ReadU64();
                        break;

                    case DwarfOperationKind.const8s:
                        op.Operand1.I64 = ReadI64();
                        break;

                    case DwarfOperationKind.constu:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.consts:
                        op.Operand1.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.deref:
                    case DwarfOperationKind.dup:
                    case DwarfOperationKind.drop:
                    case DwarfOperationKind.over:
                    case DwarfOperationKind.swap:
                    case DwarfOperationKind.rot:
                    case DwarfOperationKind.xderef:
                    case DwarfOperationKind.abs:
                    case DwarfOperationKind.and:
                    case DwarfOperationKind.div:
                    case DwarfOperationKind.minus:
                    case DwarfOperationKind.mod:
                    case DwarfOperationKind.mul:
                    case DwarfOperationKind.neg:
                    case DwarfOperationKind.not:
                    case DwarfOperationKind.or:
                    case DwarfOperationKind.plus:
                    case DwarfOperationKind.shl:
                    case DwarfOperationKind.shr:
                    case DwarfOperationKind.shra:
                    case DwarfOperationKind.xor:
                    case DwarfOperationKind.eq:
                    case DwarfOperationKind.ge:
                    case DwarfOperationKind.gt:
                    case DwarfOperationKind.le:
                    case DwarfOperationKind.lt:
                    case DwarfOperationKind.ne:
                    case DwarfOperationKind.nop:
                    case DwarfOperationKind.push_object_address:
                    case DwarfOperationKind.form_tls_address:
                    case DwarfOperationKind.call_frame_cfa:
                        break;

                    case DwarfOperationKind.pick:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.plus_uconst:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.bra:
                    case DwarfOperationKind.skip:
                        // TODO: resolve branches to DwarfOperation
                        op.Operand1.I64 = ReadI16();
                        break;

                    case DwarfOperationKind.lit0:
                    case DwarfOperationKind.lit1:
                    case DwarfOperationKind.lit2:
                    case DwarfOperationKind.lit3:
                    case DwarfOperationKind.lit4:
                    case DwarfOperationKind.lit5:
                    case DwarfOperationKind.lit6:
                    case DwarfOperationKind.lit7:
                    case DwarfOperationKind.lit8:
                    case DwarfOperationKind.lit9:
                    case DwarfOperationKind.lit10:
                    case DwarfOperationKind.lit11:
                    case DwarfOperationKind.lit12:
                    case DwarfOperationKind.lit13:
                    case DwarfOperationKind.lit14:
                    case DwarfOperationKind.lit15:
                    case DwarfOperationKind.lit16:
                    case DwarfOperationKind.lit17:
                    case DwarfOperationKind.lit18:
                    case DwarfOperationKind.lit19:
                    case DwarfOperationKind.lit20:
                    case DwarfOperationKind.lit21:
                    case DwarfOperationKind.lit22:
                    case DwarfOperationKind.lit23:
                    case DwarfOperationKind.lit24:
                    case DwarfOperationKind.lit25:
                    case DwarfOperationKind.lit26:
                    case DwarfOperationKind.lit27:
                    case DwarfOperationKind.lit28:
                    case DwarfOperationKind.lit29:
                    case DwarfOperationKind.lit30:
                    case DwarfOperationKind.lit31:
                        op.Operand1.U64 = (ulong)((byte)kind.Value - (byte)DwarfOperationKind.lit0);
                        break;

                    case DwarfOperationKind.reg0:
                    case DwarfOperationKind.reg1:
                    case DwarfOperationKind.reg2:
                    case DwarfOperationKind.reg3:
                    case DwarfOperationKind.reg4:
                    case DwarfOperationKind.reg5:
                    case DwarfOperationKind.reg6:
                    case DwarfOperationKind.reg7:
                    case DwarfOperationKind.reg8:
                    case DwarfOperationKind.reg9:
                    case DwarfOperationKind.reg10:
                    case DwarfOperationKind.reg11:
                    case DwarfOperationKind.reg12:
                    case DwarfOperationKind.reg13:
                    case DwarfOperationKind.reg14:
                    case DwarfOperationKind.reg15:
                    case DwarfOperationKind.reg16:
                    case DwarfOperationKind.reg17:
                    case DwarfOperationKind.reg18:
                    case DwarfOperationKind.reg19:
                    case DwarfOperationKind.reg20:
                    case DwarfOperationKind.reg21:
                    case DwarfOperationKind.reg22:
                    case DwarfOperationKind.reg23:
                    case DwarfOperationKind.reg24:
                    case DwarfOperationKind.reg25:
                    case DwarfOperationKind.reg26:
                    case DwarfOperationKind.reg27:
                    case DwarfOperationKind.reg28:
                    case DwarfOperationKind.reg29:
                    case DwarfOperationKind.reg30:
                    case DwarfOperationKind.reg31:
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.reg0;
                        break;

                    case DwarfOperationKind.breg0:
                    case DwarfOperationKind.breg1:
                    case DwarfOperationKind.breg2:
                    case DwarfOperationKind.breg3:
                    case DwarfOperationKind.breg4:
                    case DwarfOperationKind.breg5:
                    case DwarfOperationKind.breg6:
                    case DwarfOperationKind.breg7:
                    case DwarfOperationKind.breg8:
                    case DwarfOperationKind.breg9:
                    case DwarfOperationKind.breg10:
                    case DwarfOperationKind.breg11:
                    case DwarfOperationKind.breg12:
                    case DwarfOperationKind.breg13:
                    case DwarfOperationKind.breg14:
                    case DwarfOperationKind.breg15:
                    case DwarfOperationKind.breg16:
                    case DwarfOperationKind.breg17:
                    case DwarfOperationKind.breg18:
                    case DwarfOperationKind.breg19:
                    case DwarfOperationKind.breg20:
                    case DwarfOperationKind.breg21:
                    case DwarfOperationKind.breg22:
                    case DwarfOperationKind.breg23:
                    case DwarfOperationKind.breg24:
                    case DwarfOperationKind.breg25:
                    case DwarfOperationKind.breg26:
                    case DwarfOperationKind.breg27:
                    case DwarfOperationKind.breg28:
                    case DwarfOperationKind.breg29:
                    case DwarfOperationKind.breg30:
                    case DwarfOperationKind.breg31:
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.reg0;
                        op.Operand2.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.regx:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.fbreg:
                        op.Operand1.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.bregx:
                        op.Operand1.U64 = ReadULEB128();
                        op.Operand2.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.piece:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.deref_size:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.xderef_size:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.call2:
                    {
                        var offset = ReadU16();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.call4:
                    {
                        var offset = ReadU32();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.call_ref:
                    {
                        var offset = ReadUIntFromEncoding();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.bit_piece:
                        op.Operand1.U64 = ReadULEB128();
                        op.Operand2.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.implicit_value:
                        {
                            var length = ReadULEB128();
                            op.Operand0 = ReadAsStream(length);
                            break;
                        }

                    case DwarfOperationKind.stack_value:
                        break;

                    case DwarfOperationKind.implicit_pointer:
                    case DwarfOperationKind.GNU_implicit_pointer:
                    {
                        ulong offset;
                        //  a reference to a debugging information entry that describes the dereferenced object’s value
                        if (_version == 2)
                        {
                            offset = ReadUInt();
                        }
                        else
                        {
                            offset = ReadUIntFromEncoding();
                        }
                        //  a signed number that is treated as a byte offset from the start of that value
                        op.Operand1.I64 = ReadILEB128();

                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinSection(dieRef, false);
                        }
                        break;
                    }

                    case DwarfOperationKind.addrx:
                    case DwarfOperationKind.GNU_addr_index:
                    case DwarfOperationKind.constx:
                    case DwarfOperationKind.GNU_const_index:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.entry_value:
                    case DwarfOperationKind.GNU_entry_value:
                    {
                        var length = ReadULEB128();
                        var nested = ReadExpression(length);
                        break;
                    }

                    case DwarfOperationKind.const_type:
                    case DwarfOperationKind.GNU_const_type:
                    {
                        // The DW_OP_const_type operation takes three operands

                        // The first operand is an unsigned LEB128 integer that represents the offset
                        // of a debugging information entry in the current compilation unit, which
                        // must be a DW_TAG_base_type entry that provides the type of the constant provided
                        var offset = ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                        }
                        op.Operand1.U64 = ReadEncodedValue(kind);
                        break;
                    }

                    case DwarfOperationKind.regval_type:
                    case DwarfOperationKind.GNU_regval_type:
                    {
                        // The DW_OP_regval_type operation provides the contents of a given register
                        // interpreted as a value of a given type

                        // The first operand is an unsigned LEB128 number, which identifies a register
                        // whose contents is to be pushed onto the stack
                        op.Operand1.U64 = ReadULEB128();

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        var offset = ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                        }
                        break;
                    }

                    case DwarfOperationKind.deref_type:
                    case DwarfOperationKind.GNU_deref_type:
                    case DwarfOperationKind.xderef_type:
                    {
                        // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                        // it pops the top stack entry and treats it as an address.

                        // This operand is a 1-byte unsigned integral constant whose value which is the
                        // same as the size of the base type referenced by the second operand
                        op.Operand1.U64 = ReadU8();

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        var offset = ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                        }
                        break;
                    }

                    case DwarfOperationKind.convert:
                    case DwarfOperationKind.GNU_convert:
                    case DwarfOperationKind.reinterpret:
                    case DwarfOperationKind.GNU_reinterpret:
                    {
                        ulong offset = ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                        }
                        break;
                    }

                    case DwarfOperationKind.GNU_push_tls_address:
                    case DwarfOperationKind.GNU_uninit:
                        break;

                    case DwarfOperationKind.GNU_encoded_addr:
                        op.Operand1.U64 = ReadEncodedValue(kind);
                        break;

                    case DwarfOperationKind.GNU_parameter_ref:
                        op.Operand1.U64 = ReadU32();
                        break;

                    default:
                        throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {kind} is not supported");
                }

                // Store the size of the current op
                op.Size = Offset - op.Offset;
            }

            return exprLoc;
        }

        private ulong ReadEncodedValue(DwarfOperationKind kind)
        {
            var size = ReadU8();
            switch (size)
            {
                case 0:
                    return ReadUInt();
                case 1:
                    return ReadU8();
                case 2:
                    return ReadU16();
                case 4:
                    return ReadU32();
                case 8:
                    return ReadU64();
                default:
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        private void ResolveAttributeReferenceWithinCompilationUnit(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerCompilationUnit.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        _diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}, section 0x{(dieRef.Offset + _offsetOfCompilationUnitInSection):x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _unresolvedDIECompilationUnitReference.Add(dieRef);
                }
            }
        }

        private void ResolveAttributeReferenceWithinSection(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerSection.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        _diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _attributesWithUnresolvedDIESectionReference.Add(dieRef);
                }
            }
        }

        private struct DwarfCompilationUnitHeader
        {
            public ulong unit_length;

            public ushort version;

            public byte unit_type;

            public ulong debug_abbrev_offset;

            public byte address_size;
        }

        private static readonly DwarfDIEReferenceResolver DwarfAttributeDIEReferenceResolverInstance = DwarfAttributeDIEReferenceResolver;

        private static DwarfDIEReference AttributeToDIERef(DwarfAttribute attr)
        {
            return new DwarfDIEReference(attr.ValueAsU64, attr, DwarfAttributeDIEReferenceResolverInstance);
        }

        private static void DwarfAttributeDIEReferenceResolver(ref DwarfDIEReference dieRef)
        {
            var attr = (DwarfAttribute) dieRef.DwarfObject;
            attr.ValueAsU64 = 0;
            attr.ValueAsObject = dieRef.Resolved;
        }


        private static readonly DwarfDIEReferenceResolver DwarfExpressionLocationDIEReferenceResolverInstance = DwarfExpressionLocationDIEReferenceResolver;

        private static void DwarfExpressionLocationDIEReferenceResolver(ref DwarfDIEReference dieRef)
        {
            var op = (DwarfOperation)dieRef.DwarfObject;
            op.Operand0 = dieRef.Resolved;
        }

        private struct DwarfDIEReference
        {
            public DwarfDIEReference(ulong offset, object dwarfObject, DwarfDIEReferenceResolver resolver) : this()
            {
                Offset = offset;
                DwarfObject = dwarfObject;
                Resolver = resolver;
            }

            public ulong Offset;

            public object DwarfObject;

            public DwarfDIEReferenceResolver Resolver;

            public DwarfDIE Resolved;
        }

        private delegate void DwarfDIEReferenceResolver(ref DwarfDIEReference reference);
    }
}