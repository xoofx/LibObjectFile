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
        private readonly Dictionary<ulong, DwarfLine> _offsetToDebugLine;
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
            _offsetToDebugLine = new Dictionary<ulong, DwarfLine>();
        }

        public override bool IsReadOnly { get; }

        public new DwarfReaderContext Context => (DwarfReaderContext)base.Context;
        
        internal void Read(DwarfInfoSection debugInfo, DwarfUnitKind defaultUnitKind)
        {
            _diagnostics = Diagnostics;
            _parent = debugInfo.Parent;

            // Prebuild map offset to debug line
            if (_parent.LineSection != null)
            {
                foreach(var debugLine in _parent.LineSection.Lines)
                {
                    _offsetToDebugLine.Add(debugLine.Offset, debugLine);
                }
            }

            var addressRangeTable = debugInfo.Parent.AddressRangeTable;

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

                // Link AddressRangeTable to Unit
                if (addressRangeTable != null && addressRangeTable.DebugInfoOffset == cu.Offset)
                {
                    addressRangeTable.Unit = cu;
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
            _parent.AbbreviationTable.AddAbbreviation(abbreviation);

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
            
            die.Abbrev = abbreviationItem;
            die.Offset = startDIEOffset ;
            die.Tag = abbreviationItem.Tag;

            // Store map offset to DIE to resolve references
            _registeredDIEPerCompilationUnit.Add(startDIEOffset - _offsetOfCompilationUnitInSection, die);
            _registeredDIEPerSection.Add(startDIEOffset, die);

            // Console.WriteLine($" <{level}><{die.Offset:x}> Abbrev Number: {abbreviationCode} ({die.Tag})");

            var descriptors = abbreviationItem.Descriptors;
            if (descriptors.Length > 0)
            {
                for(int i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];

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
                case DwarfAttributeKind.DeclFile:
                {
                    var file = _parent.LineSection.FileNames[attr.ValueAsI32 - 1];
                    attr.ValueAsU64 = 0;
                    attr.ValueAsObject = file;
                    break;
                }

                case DwarfAttributeKind.StmtList:
                {
                    if (attr.ValueAsU64 == 0) return;

                    if (_parent.LineSection != null)
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
                case DwarfUnitKind.Compile:
                case DwarfUnitKind.Partial:
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
                case DwarfAttributeForm.Addr:
                {
                    attr.ValueAsU64 = _is64Address ? ReadU64() : ReadU32();
                    break;
                }

                case DwarfAttributeForm.Data1:
                {
                    attr.ValueAsU64 = ReadU8();
                    break;
                }
                case DwarfAttributeForm.Data2:
                {
                    attr.ValueAsU64 = ReadU16();
                    break;
                }
                case DwarfAttributeForm.Data4:
                {
                    attr.ValueAsU64 = ReadU32();
                    break;
                }
                case DwarfAttributeForm.Data8:
                {
                    attr.ValueAsU64 = ReadU64();
                    break;
                }

                case DwarfAttributeForm.String:
                {
                    attr.ValueAsObject = ReadStringUTF8NullTerminated();
                    break;
                }

                case DwarfAttributeForm.Block:
                {
                    var length = ReadULEB128();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block1:
                {
                    var length = ReadU8();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block2:
                {
                    var length = ReadU16();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }
                case DwarfAttributeForm.Block4:
                {
                    var length = ReadU32();
                    attr.ValueAsObject = ReadAsStream(length);
                    break;
                }

                case DwarfAttributeForm.Flag:
                {
                    attr.ValueAsBoolean = ReadU8() != 0;
                    break;
                }
                case DwarfAttributeForm.Sdata:
                {
                    attr.ValueAsI64 = ReadILEB128();
                    break;
                }
                case DwarfAttributeForm.Strp:
                {
                    var offset = ReadUIntFromEncoding();
                    if (_parent.StringTable == null)
                    {
                        attr.ValueAsU64 = offset;
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_MissingStringTable, $"The .debug_str {nameof(DwarfFile.StringTable)} is null while a DW_FORM_strp for attribute {attr.Kind} is requesting an access to it");
                    }
                    else
                    {
                        attr.ValueAsObject = _parent.StringTable.GetStringFromOffset(offset);
                    }

                    break;
                }
                case DwarfAttributeForm.Udata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    break;
                }
                case DwarfAttributeForm.RefAddr:
                {
                    attr.ValueAsU64 = ReadUIntFromEncoding();
                    ResolveAttributeReferenceWithinSection(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.Ref1:
                {
                    attr.ValueAsU64 = ReadU8();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.Ref2:
                {
                    attr.ValueAsU64 = ReadU16();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.Ref4:
                {
                    attr.ValueAsU64 = ReadU32();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.Ref8:
                {
                    attr.ValueAsU64 = ReadU64();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.RefUdata:
                {
                    attr.ValueAsU64 = ReadULEB128();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), false);
                    break;
                }
                case DwarfAttributeForm.Indirect:
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
                case DwarfAttributeForm.SecOffset:
                {
                    attr.ValueAsU64 = ReadUIntFromEncoding();
                    //Console.WriteLine($"attribute {attr.Key} offset: {attr.ValueAsU64}");
                    break;
                }

                case DwarfAttributeForm.Exprloc:
                {
                    var length = ReadULEB128();
                    attr.ValueAsObject = ReadExpression(length);
                    break;
                }

                case DwarfAttributeForm.FlagPresent:
                {
                    attr.ValueAsBoolean = true;
                    break;
                }

                case DwarfAttributeForm.RefSig8:
                {
                    var offset = ReadU64();
                    attr.ValueAsU64 = offset;
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
                    case DwarfOperationKind.Addr:
                        op.Operand1.U64 = ReadUInt();
                        break;
                    case DwarfOperationKind.Const1u:
                        op.Operand1.U64 = ReadU8();
                        break;
                    case DwarfOperationKind.Const1s:
                        op.Operand1.I64 = ReadI8();
                        break;
                    case DwarfOperationKind.Const2u:
                        op.Operand1.U64 = ReadU16();
                        break;
                    case DwarfOperationKind.Const2s:
                        op.Operand1.I64 = ReadI16();
                        break;

                    case DwarfOperationKind.Const4u:
                        op.Operand1.U64 = ReadU32();
                        break;
                    case DwarfOperationKind.Const4s:
                        op.Operand1.I64 = ReadU32();
                        break;

                    case DwarfOperationKind.Const8u:
                        op.Operand1.U64 = ReadU64();
                        break;

                    case DwarfOperationKind.Const8s:
                        op.Operand1.I64 = ReadI64();
                        break;

                    case DwarfOperationKind.Constu:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.Consts:
                        op.Operand1.I64 = ReadILEB128();
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

                    case DwarfOperationKind.Pick:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.PlusUconst:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.Bra:
                    case DwarfOperationKind.Skip:
                        // TODO: resolve branches to DwarfOperation
                        op.Operand1.I64 = ReadI16();
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
                        op.Operand1.U64 = (ulong)((byte)kind.Value - (byte)DwarfOperationKind.Lit0);
                        break;

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
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Reg0;
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
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Breg0;
                        op.Operand2.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.Regx:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.Fbreg:
                        op.Operand1.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.Bregx:
                        op.Operand1.U64 = ReadULEB128();
                        op.Operand2.I64 = ReadILEB128();
                        break;

                    case DwarfOperationKind.Piece:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.DerefSize:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.XderefSize:
                        op.Operand1.U64 = ReadU8();
                        break;

                    case DwarfOperationKind.Call2:
                    {
                        var offset = ReadU16();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.Call4:
                    {
                        var offset = ReadU32();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.CallRef:
                    {
                        var offset = ReadUIntFromEncoding();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, false);
                        break;
                    }

                    case DwarfOperationKind.BitPiece:
                        op.Operand1.U64 = ReadULEB128();
                        op.Operand2.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.ImplicitValue:
                        {
                            var length = ReadULEB128();
                            op.Operand0 = ReadAsStream(length);
                            break;
                        }

                    case DwarfOperationKind.StackValue:
                        break;

                    case DwarfOperationKind.ImplicitPointer:
                    case DwarfOperationKind.GNUImplicitPointer:
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

                    case DwarfOperationKind.Addrx:
                    case DwarfOperationKind.GNUAddrIndex:
                    case DwarfOperationKind.Constx:
                    case DwarfOperationKind.GNUConstIndex:
                        op.Operand1.U64 = ReadULEB128();
                        break;

                    case DwarfOperationKind.EntryValue:
                    case DwarfOperationKind.GNUEntryValue:
                    {
                        var length = ReadULEB128();
                        op.Operand0 = ReadExpression(length);
                        break;
                    }

                    case DwarfOperationKind.ConstType:
                    case DwarfOperationKind.GNUConstType:
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
                        op.Operand1.U64 = ReadEncodedValue(kind, out var sizeOfEncodedValue);
                        // Encode size of encoded value in Operand1
                        op.Operand2.U64 = sizeOfEncodedValue;
                        break;
                    }

                    case DwarfOperationKind.RegvalType:
                    case DwarfOperationKind.GNURegvalType:
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

                    case DwarfOperationKind.DerefType:
                    case DwarfOperationKind.GNUDerefType:
                    case DwarfOperationKind.XderefType:
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

                    case DwarfOperationKind.Convert:
                    case DwarfOperationKind.GNUConvert:
                    case DwarfOperationKind.Reinterpret:
                    case DwarfOperationKind.GNUReinterpret:
                    {
                        ulong offset = ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                        }
                        break;
                    }

                    case DwarfOperationKind.GNUPushTlsAddress:
                    case DwarfOperationKind.GNUUninit:
                        break;

                    case DwarfOperationKind.GNUEncodedAddr:
                    {
                        op.Operand1.U64 = ReadEncodedValue(kind, out var sizeOfEncodedValue);
                        op.Operand2.U64 = sizeOfEncodedValue;
                        break;
                    }

                    case DwarfOperationKind.GNUParameterRef:
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

        private ulong ReadEncodedValue(DwarfOperationKind kind, out byte size)
        {
            size = ReadU8();
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