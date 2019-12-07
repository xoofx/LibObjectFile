// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfDebugInfoSection : DwarfSection
    {
        private readonly List<DwarfUnit> _units;

        public DwarfDebugInfoSection()
        {
            _units = new List<DwarfUnit>();
        }
        
        public IReadOnlyList<DwarfUnit> Units => _units;
        
        public void AddUnit(DwarfUnit unit)
        {
            _units.Add<DwarfContainer, DwarfUnit>(this, unit);
        }

        internal void Read(DwarfReaderWriter reader)
        {
            if (reader.FileContext.DebugInfoStream.Stream == null)
            {
                return;
            }

            var internalContext = DebugInfoReaderContext.Create(Parent.DebugLineSection?.Lines.Count ?? 0);
            internalContext.Diagnostics = reader.Diagnostics;

            // Prebuild map offset to debug line
            if (Parent.DebugLineSection != null)
            {
                foreach(var debugLine in Parent.DebugLineSection.Lines)
                {
                    internalContext.OffsetToDebugLine.Add(debugLine.Offset, debugLine);
                }
            }
            
            while (true)
            {
                if (reader.Offset >= reader.Length)
                {
                    break;
                }
                // 7.5 Format of Debugging Information
                // - Each such contribution consists of a compilation unit header

                var startOffset = reader.Offset;
                if (!TryReadCompilationUnitHeader(reader, out var header, out var offsetEndOfUnit))
                {
                    reader.Offset = offsetEndOfUnit;
                    continue;
                }

                var cu = new DwarfCompilationUnit
                {
                    Offset = (ulong)startOffset,
                    Is64BitEncoding = reader.Is64BitEncoding,
                    Version = header.version,
                };

                if (header.address_size != 4 && header.address_size != 8)
                {
                    reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Unsupported address size {header.address_size} for compilation unit at offset {startOffset} in {this}. Must be 4 (32 bits) or 8 (64 bits).");
                    return;
                }
                cu.Is64BitAddress = header.address_size == 8;

                internalContext.Version = header.version;
                internalContext.OffsetOfCompilationUnitInSection = startOffset;
                internalContext.RegisteredDIEPerCompilationUnit.Clear();
                internalContext.UnresolvedDIECompilationUnitReference.Clear();
                
                internalContext.Is64Address = header.address_size == 8;

                var abbreviation = ReadAbbreviation(reader.FileContext.DebugAbbrevStream, header.debug_abbrev_offset, internalContext);
                
                // Each debugging information entry begins with an unsigned LEB128 number containing the abbreviation code for the entry.
                cu.Abbreviation = abbreviation;
                cu.Root = ReadDIE(reader, ref internalContext, abbreviation, 0);

                // Resolve attribute reference within the CU
                foreach (var unresolvedAttrRef in internalContext.UnresolvedDIECompilationUnitReference)
                {
                    ResolveAttributeReferenceWithinCompilationUnit(unresolvedAttrRef, internalContext, true);
                }

                AddUnit(cu);
            }

            // Resolve attribute reference within the section
            foreach (var unresolvedAttrRef in internalContext.AttributesWithUnresolvedDIESectionReference)
            {
                ResolveAttributeReferenceWithinSection(unresolvedAttrRef, internalContext, true);
            }
        }

        private DwarfAbbreviation ReadAbbreviation(Stream stream, ulong abbreviationOffset, in DebugInfoReaderContext context)
        {
            if (context.Abbreviations.TryGetValue(abbreviationOffset, out var abbreviation))
            {
                return abbreviation;
            }

            abbreviation = DwarfAbbreviation.Read(stream, abbreviationOffset);
            Parent.DebugAbbrevTable.AddAbbreviation(abbreviation);

            context.Abbreviations[abbreviationOffset] = abbreviation;
            return abbreviation;
        }
        
        private DwarfDIE ReadDIE(DwarfReaderWriter reader, ref DebugInfoReaderContext internalContext, DwarfAbbreviation abbreviation, int level)
        {
            var startDIEOffset = reader.Offset;
            var abbreviationCode = reader.ReadULEB128();

            if (abbreviationCode == 0)
            {
                return null;
            }

            if (!abbreviation.TryFindByCode(abbreviationCode, out var abbreviationItem))
            {
                throw new InvalidOperationException($"Invalid abbreviation code {abbreviationCode}");
            }

            var die = new DwarfDIE
            {
                Offset = startDIEOffset,
                Tag = abbreviationItem.Tag
            };

            // Store map offset to DIE to resolve references
            internalContext.RegisteredDIEPerCompilationUnit.Add(startDIEOffset - internalContext.OffsetOfCompilationUnitInSection, die);
            internalContext.RegisteredDIEPerSection.Add(startDIEOffset, die);

            // Console.WriteLine($" <{level}><{die.Offset:x}> Abbrev Number: {abbreviationCode} ({die.Tag})");

            if (abbreviationItem.Descriptors != null)
            {
                foreach (var descriptor in abbreviationItem.Descriptors)
                {
                    var attribute = new DwarfAttribute()
                    {
                        Offset = reader.Offset,
                        Key = descriptor.Key,
                    };
                    var form = descriptor.Form;
                    ReadAttributeFormRawValue(reader, form, attribute, internalContext);

                    attribute.Size = reader.Offset - attribute.Offset;

                    ResolveAttributeValue(attribute, ref internalContext);
                    
                    die.AddAttribute(attribute);
                }
            }

            if (abbreviationItem.HasChildren)
            {
                while (true)
                {
                    var child = ReadDIE(reader, ref internalContext, abbreviation, level+1);
                    if (child == null) break;
                    die.AddChild(child);
                }
            }

            die.Size = reader.Offset - startDIEOffset;

            return die;
        }

        private void ResolveAttributeValue(DwarfAttribute attr, ref DebugInfoReaderContext internalContext)
        {
            switch (attr.Key.Value)
            {
                case DwarfNative.DW_AT_decl_file:
                {
                    var file = Parent.DebugLineSection.FileNames[attr.ValueAsI32 - 1];
                    attr.ValueAsU64 = 0;
                    attr.ValueAsObject = file;
                    break;
                }
                case DwarfNative.DW_AT_stmt_list:
                {
                    if (attr.ValueAsU64 == 0) return;

                    if (Parent.DebugLineSection != null)
                    {
                        if (internalContext.OffsetToDebugLine.TryGetValue(attr.ValueAsU64, out var debugLine))
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
        
        private bool TryReadCompilationUnitHeader(DwarfReaderWriter reader, out DwarfCompilationUnitHeader header, out ulong offsetEndOfUnit)
        {
            header = new DwarfCompilationUnitHeader();

            // 1. unit_length 
            header.unit_length = reader.ReadUnitLength();

            offsetEndOfUnit = (ulong)reader.Offset + header.unit_length;

            // 2. version (uhalf) 
            header.version = reader.ReadU16();

            if (header.version <= 2 || header.version > 5)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {header.version} is not supported");
                return false;
            }
            if (header.version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                header.debug_abbrev_offset = reader.ReadUIntFromEncoding();

                // 4. address_size (ubyte) 
                header.address_size = reader.ReadU8();
            }
            else
            {
                // 3. unit_type (ubyte)
                header.unit_type = reader.ReadU8();

                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                header.address_size = reader.ReadU8();

                // 5. debug_abbrev_offset (section offset) 
                header.debug_abbrev_offset = reader.ReadUIntFromEncoding();

            }

            return true;
        }

        private void ReadAttributeFormRawValue(DwarfReaderWriter reader, DwarfAttributeForm attributeForm, DwarfAttribute attr, in DebugInfoReaderContext context)
        {
            indirect:
            switch (attributeForm.Value)
            {
                case DwarfNative.DW_FORM_addr:
                {
                    attr.ValueAsU64 = context.Is64Address ? reader.ReadU64() : reader.ReadU32();
                    break;
                }

                case DwarfNative.DW_FORM_data1:
                {
                    attr.ValueAsU64 = reader.ReadU8();
                    break;
                }
                case DwarfNative.DW_FORM_data2:
                {
                    attr.ValueAsU64 = reader.ReadU16();
                    break;
                }
                case DwarfNative.DW_FORM_data4:
                {
                    attr.ValueAsU64 = reader.ReadU32();
                    break;
                }
                case DwarfNative.DW_FORM_data8:
                {
                    attr.ValueAsU64 = reader.ReadU64();
                    break;
                }

                case DwarfNative.DW_FORM_string:
                {
                    attr.ValueAsObject = reader.ReadStringUTF8NullTerminated();
                    break;
                }

                case DwarfNative.DW_FORM_block:
                {
                    var length = reader.ReadULEB128();
                    attr.ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfNative.DW_FORM_block1:
                {
                    var length = reader.ReadU8();
                    attr.ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfNative.DW_FORM_block2:
                {
                    var length = reader.ReadU16();
                    attr.ValueAsObject = reader.ReadAsStream(length);
                    break;
                }
                case DwarfNative.DW_FORM_block4:
                {
                    var length = reader.ReadU32();
                    attr.ValueAsObject = reader.ReadAsStream(length);
                    break;
                }

                case DwarfNative.DW_FORM_flag:
                {
                    attr.ValueAsBoolean = reader.ReadU8() != 0;
                    break;
                }
                case DwarfNative.DW_FORM_sdata:
                {
                    attr.ValueAsI64 = reader.ReadILEB128();
                    break;
                }
                case DwarfNative.DW_FORM_strp:
                {
                    var offset = reader.ReadUIntFromEncoding();
                    if (Parent.DebugStringTable == null)
                    {
                        attr.ValueAsU64 = offset;
                        reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_MissingStringTable, $"The .debug_str {nameof(DwarfFile.DebugStringTable)} is null while a DW_FORM_strp for attribute {attr.Key} is requesting an access to it");
                    }
                    else
                    {
                        attr.ValueAsObject = Parent.DebugStringTable.GetStringFromOffset(offset);
                    }

                    break;
                }
                case DwarfNative.DW_FORM_udata:
                {
                    attr.ValueAsU64 = reader.ReadULEB128();
                    break;
                }
                case DwarfNative.DW_FORM_ref_addr:
                {
                    attr.ValueAsU64 = reader.ReadUIntFromEncoding();
                    ResolveAttributeReferenceWithinSection(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_ref1:
                {
                    attr.ValueAsU64 = reader.ReadU8();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_ref2:
                {
                    attr.ValueAsU64 = reader.ReadU16();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_ref4:
                {
                    attr.ValueAsU64 = reader.ReadU32();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_ref8:
                {
                    attr.ValueAsU64 = reader.ReadU64();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_ref_udata:
                {
                    attr.ValueAsU64 = reader.ReadULEB128();
                    ResolveAttributeReferenceWithinCompilationUnit(AttributeToDIERef(attr), context, false);
                    break;
                }
                case DwarfNative.DW_FORM_indirect:
                {
                    attributeForm = reader.ReadLEB128As<DwarfAttributeForm>();
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
                case DwarfNative.DW_FORM_sec_offset:
                {
                    attr.ValueAsU64 = reader.ReadUIntFromEncoding();
                    //Console.WriteLine($"attribute {attr.Key} offset: {attr.ValueAsU64}");
                    break;
                }

                case DwarfNative.DW_FORM_exprloc:
                {
                    var length = reader.ReadULEB128();
                    attr.ValueAsObject = ReadExpression(reader, length, context);
                    break;
                }

                case DwarfNative.DW_FORM_flag_present:
                {
                    attr.ValueAsBoolean = true;
                    break;
                }

                case DwarfNative.DW_FORM_ref_sig8:
                {
                    var offset = reader.ReadU64();
                    attr.ValueAsU64 = offset;
                    break;
                }

                case DwarfNative.DW_FORM_strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfNative.DW_FORM_addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfNative.DW_FORM_ref_sup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfNative.DW_FORM_strp_sup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfNative.DW_FORM_data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfNative.DW_FORM_line_strp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfNative.DW_FORM_implicit_const: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfNative.DW_FORM_loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfNative.DW_FORM_rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfNative.DW_FORM_ref_sup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfNative.DW_FORM_strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfNative.DW_FORM_strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfNative.DW_FORM_strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfNative.DW_FORM_strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfNative.DW_FORM_addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfNative.DW_FORM_addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfNative.DW_FORM_addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfNative.DW_FORM_addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfNative.DW_FORM_GNU_addr_index: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfNative.DW_FORM_GNU_str_index: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfNative.DW_FORM_GNU_ref_alt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfNative.DW_FORM_GNU_strp_alt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default:
                    throw new NotSupportedException($"Unknown {nameof(DwarfAttributeForm)}: {attributeForm.Value}");
            }
        }

        private DwarfExpression ReadExpression(DwarfReaderWriter reader, ulong size, in DebugInfoReaderContext context)
        {
            var stream = reader.Stream;
            var startPosition = (ulong)stream.Position;
            var exprLoc = new DwarfExpression {Offset = startPosition, Size = size};
            var endPosition = startPosition + size;

            while ((ulong)stream.Position < endPosition)
            {
                var kind = new DwarfOperationKind(stream.ReadU8());
                var op = new DwarfOperation
                {
                    Offset = reader.Offset,
                    Kind = kind
                };
                exprLoc.AddOperation(op);

                switch (kind.Value)
                {
                    case DwarfNative.DW_OP_addr:
                        op.Operand1.U64 = reader.ReadUInt();
                        break;
                    case DwarfNative.DW_OP_const1u:
                        op.Operand1.U64 = reader.ReadU8();
                        break;
                    case DwarfNative.DW_OP_const1s:
                        op.Operand1.I64 = reader.ReadI8();
                        break;
                    case DwarfNative.DW_OP_const2u:
                        op.Operand1.U64 = reader.ReadU16();
                        break;
                    case DwarfNative.DW_OP_const2s:
                        op.Operand1.I64 = reader.ReadI16();
                        break;

                    case DwarfNative.DW_OP_const4u:
                        op.Operand1.U64 = reader.ReadU32();
                        break;
                    case DwarfNative.DW_OP_const4s:
                        op.Operand1.I64 = reader.ReadU32();
                        break;

                    case DwarfNative.DW_OP_const8u:
                        op.Operand1.U64 = reader.ReadU64();
                        break;

                    case DwarfNative.DW_OP_const8s:
                        op.Operand1.I64 = reader.ReadI64();
                        break;

                    case DwarfNative.DW_OP_constu:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_consts:
                        op.Operand1.I64 = reader.ReadILEB128();
                        break;

                    case DwarfNative.DW_OP_deref:
                    case DwarfNative.DW_OP_dup:
                    case DwarfNative.DW_OP_drop:
                    case DwarfNative.DW_OP_over:
                    case DwarfNative.DW_OP_swap:
                    case DwarfNative.DW_OP_rot:
                    case DwarfNative.DW_OP_xderef:
                    case DwarfNative.DW_OP_abs:
                    case DwarfNative.DW_OP_and:
                    case DwarfNative.DW_OP_div:
                    case DwarfNative.DW_OP_minus:
                    case DwarfNative.DW_OP_mod:
                    case DwarfNative.DW_OP_mul:
                    case DwarfNative.DW_OP_neg:
                    case DwarfNative.DW_OP_not:
                    case DwarfNative.DW_OP_or:
                    case DwarfNative.DW_OP_plus:
                    case DwarfNative.DW_OP_shl:
                    case DwarfNative.DW_OP_shr:
                    case DwarfNative.DW_OP_shra:
                    case DwarfNative.DW_OP_xor:
                    case DwarfNative.DW_OP_eq:
                    case DwarfNative.DW_OP_ge:
                    case DwarfNative.DW_OP_gt:
                    case DwarfNative.DW_OP_le:
                    case DwarfNative.DW_OP_lt:
                    case DwarfNative.DW_OP_ne:
                    case DwarfNative.DW_OP_nop:
                    case DwarfNative.DW_OP_push_object_address:
                    case DwarfNative.DW_OP_form_tls_address:
                    case DwarfNative.DW_OP_call_frame_cfa:
                        break;

                    case DwarfNative.DW_OP_pick:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfNative.DW_OP_plus_uconst:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_bra:
                    case DwarfNative.DW_OP_skip:
                        // TODO: resolve branches to DwarfOperation
                        op.Operand1.I64 = reader.ReadI16();
                        break;

                    case DwarfNative.DW_OP_lit0:
                    case DwarfNative.DW_OP_lit1:
                    case DwarfNative.DW_OP_lit2:
                    case DwarfNative.DW_OP_lit3:
                    case DwarfNative.DW_OP_lit4:
                    case DwarfNative.DW_OP_lit5:
                    case DwarfNative.DW_OP_lit6:
                    case DwarfNative.DW_OP_lit7:
                    case DwarfNative.DW_OP_lit8:
                    case DwarfNative.DW_OP_lit9:
                    case DwarfNative.DW_OP_lit10:
                    case DwarfNative.DW_OP_lit11:
                    case DwarfNative.DW_OP_lit12:
                    case DwarfNative.DW_OP_lit13:
                    case DwarfNative.DW_OP_lit14:
                    case DwarfNative.DW_OP_lit15:
                    case DwarfNative.DW_OP_lit16:
                    case DwarfNative.DW_OP_lit17:
                    case DwarfNative.DW_OP_lit18:
                    case DwarfNative.DW_OP_lit19:
                    case DwarfNative.DW_OP_lit20:
                    case DwarfNative.DW_OP_lit21:
                    case DwarfNative.DW_OP_lit22:
                    case DwarfNative.DW_OP_lit23:
                    case DwarfNative.DW_OP_lit24:
                    case DwarfNative.DW_OP_lit25:
                    case DwarfNative.DW_OP_lit26:
                    case DwarfNative.DW_OP_lit27:
                    case DwarfNative.DW_OP_lit28:
                    case DwarfNative.DW_OP_lit29:
                    case DwarfNative.DW_OP_lit30:
                    case DwarfNative.DW_OP_lit31:
                        op.Operand1.U64 = (ulong)kind.Value - DwarfNative.DW_OP_lit0;
                        break;

                    case DwarfNative.DW_OP_reg0:
                    case DwarfNative.DW_OP_reg1:
                    case DwarfNative.DW_OP_reg2:
                    case DwarfNative.DW_OP_reg3:
                    case DwarfNative.DW_OP_reg4:
                    case DwarfNative.DW_OP_reg5:
                    case DwarfNative.DW_OP_reg6:
                    case DwarfNative.DW_OP_reg7:
                    case DwarfNative.DW_OP_reg8:
                    case DwarfNative.DW_OP_reg9:
                    case DwarfNative.DW_OP_reg10:
                    case DwarfNative.DW_OP_reg11:
                    case DwarfNative.DW_OP_reg12:
                    case DwarfNative.DW_OP_reg13:
                    case DwarfNative.DW_OP_reg14:
                    case DwarfNative.DW_OP_reg15:
                    case DwarfNative.DW_OP_reg16:
                    case DwarfNative.DW_OP_reg17:
                    case DwarfNative.DW_OP_reg18:
                    case DwarfNative.DW_OP_reg19:
                    case DwarfNative.DW_OP_reg20:
                    case DwarfNative.DW_OP_reg21:
                    case DwarfNative.DW_OP_reg22:
                    case DwarfNative.DW_OP_reg23:
                    case DwarfNative.DW_OP_reg24:
                    case DwarfNative.DW_OP_reg25:
                    case DwarfNative.DW_OP_reg26:
                    case DwarfNative.DW_OP_reg27:
                    case DwarfNative.DW_OP_reg28:
                    case DwarfNative.DW_OP_reg29:
                    case DwarfNative.DW_OP_reg30:
                    case DwarfNative.DW_OP_reg31:
                        op.Operand1.U64 = (ulong)kind.Value - DwarfNative.DW_OP_reg0;
                        break;

                    case DwarfNative.DW_OP_breg0:
                    case DwarfNative.DW_OP_breg1:
                    case DwarfNative.DW_OP_breg2:
                    case DwarfNative.DW_OP_breg3:
                    case DwarfNative.DW_OP_breg4:
                    case DwarfNative.DW_OP_breg5:
                    case DwarfNative.DW_OP_breg6:
                    case DwarfNative.DW_OP_breg7:
                    case DwarfNative.DW_OP_breg8:
                    case DwarfNative.DW_OP_breg9:
                    case DwarfNative.DW_OP_breg10:
                    case DwarfNative.DW_OP_breg11:
                    case DwarfNative.DW_OP_breg12:
                    case DwarfNative.DW_OP_breg13:
                    case DwarfNative.DW_OP_breg14:
                    case DwarfNative.DW_OP_breg15:
                    case DwarfNative.DW_OP_breg16:
                    case DwarfNative.DW_OP_breg17:
                    case DwarfNative.DW_OP_breg18:
                    case DwarfNative.DW_OP_breg19:
                    case DwarfNative.DW_OP_breg20:
                    case DwarfNative.DW_OP_breg21:
                    case DwarfNative.DW_OP_breg22:
                    case DwarfNative.DW_OP_breg23:
                    case DwarfNative.DW_OP_breg24:
                    case DwarfNative.DW_OP_breg25:
                    case DwarfNative.DW_OP_breg26:
                    case DwarfNative.DW_OP_breg27:
                    case DwarfNative.DW_OP_breg28:
                    case DwarfNative.DW_OP_breg29:
                    case DwarfNative.DW_OP_breg30:
                    case DwarfNative.DW_OP_breg31:
                        op.Operand1.U64 = (ulong)kind.Value - DwarfNative.DW_OP_reg0;
                        op.Operand2.I64 = reader.ReadILEB128();
                        break;

                    case DwarfNative.DW_OP_regx:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_fbreg:
                        op.Operand1.I64 = reader.ReadILEB128();
                        break;

                    case DwarfNative.DW_OP_bregx:
                        op.Operand1.U64 = reader.ReadULEB128();
                        op.Operand2.I64 = reader.ReadILEB128();
                        break;

                    case DwarfNative.DW_OP_piece:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_deref_size:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfNative.DW_OP_xderef_size:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfNative.DW_OP_call2:
                    {
                        var offset = reader.ReadU16();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, context, false);
                        break;
                    }

                    case DwarfNative.DW_OP_call4:
                    {
                        var offset = reader.ReadU32();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, context, false);
                        break;
                    }

                    case DwarfNative.DW_OP_call_ref:
                    {
                        var offset = reader.ReadUIntFromEncoding();
                        var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                        ResolveAttributeReferenceWithinSection(dieRef, context, false);
                        break;
                    }

                    case DwarfNative.DW_OP_bit_piece:
                        op.Operand1.U64 = reader.ReadULEB128();
                        op.Operand2.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_implicit_value:
                        {
                            var length = reader.ReadULEB128();
                            op.Operand0 = reader.ReadAsStream(length);
                            break;
                        }

                    case DwarfNative.DW_OP_stack_value:
                        break;

                    case DwarfNative.DW_OP_implicit_pointer:
                    case DwarfNative.DW_OP_GNU_implicit_pointer:
                    {
                        ulong offset;
                        //  a reference to a debugging information entry that describes the dereferenced object’s value
                        if (context.Version == 2)
                        {
                            offset = reader.ReadUInt();
                        }
                        else
                        {
                            offset = reader.ReadUIntFromEncoding();
                        }
                        //  a signed number that is treated as a byte offset from the start of that value
                        op.Operand1.I64 = reader.ReadILEB128();

                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinSection(dieRef, context, false);
                        }
                        break;
                    }

                    case DwarfNative.DW_OP_addrx:
                    case DwarfNative.DW_OP_GNU_addr_index:
                    case DwarfNative.DW_OP_constx:
                    case DwarfNative.DW_OP_GNU_const_index:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfNative.DW_OP_entry_value:
                    case DwarfNative.DW_OP_GNU_entry_value:
                    {
                        var length = reader.ReadULEB128();
                        var nested = ReadExpression(reader, length, context);
                        break;
                    }

                    case DwarfNative.DW_OP_const_type:
                    case DwarfNative.DW_OP_GNU_const_type:
                    {
                        // The DW_OP_const_type operation takes three operands

                        // The first operand is an unsigned LEB128 integer that represents the offset
                        // of a debugging information entry in the current compilation unit, which
                        // must be a DW_TAG_base_type entry that provides the type of the constant provided
                        var offset = reader.ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, context, false);
                        }
                        op.Operand1.U64 = ReadEncodedValue(reader, kind);
                        break;
                    }

                    case DwarfNative.DW_OP_regval_type:
                    case DwarfNative.DW_OP_GNU_regval_type:
                    {
                        // The DW_OP_regval_type operation provides the contents of a given register
                        // interpreted as a value of a given type

                        // The first operand is an unsigned LEB128 number, which identifies a register
                        // whose contents is to be pushed onto the stack
                        op.Operand1.U64 = reader.ReadULEB128();

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        var offset = reader.ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, context, false);
                        }
                        break;
                    }

                    case DwarfNative.DW_OP_deref_type:
                    case DwarfNative.DW_OP_GNU_deref_type:
                    case DwarfNative.DW_OP_xderef_type:
                    {
                        // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                        // it pops the top stack entry and treats it as an address.

                        // This operand is a 1-byte unsigned integral constant whose value which is the
                        // same as the size of the base type referenced by the second operand
                        op.Operand1.U64 = reader.ReadU8();

                        // The second operand is an unsigned LEB128 number that represents the offset
                        // of a debugging information entry in the current compilation unit
                        var offset = reader.ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, context, false);
                        }
                        break;
                    }

                    case DwarfNative.DW_OP_convert:
                    case DwarfNative.DW_OP_GNU_convert:
                    case DwarfNative.DW_OP_reinterpret:
                    case DwarfNative.DW_OP_GNU_reinterpret:
                    {
                        ulong offset = reader.ReadULEB128();
                        if (offset != 0)
                        {
                            var dieRef = new DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            ResolveAttributeReferenceWithinCompilationUnit(dieRef, context, false);
                        }
                        break;
                    }

                    case DwarfNative.DW_OP_GNU_push_tls_address:
                    case DwarfNative.DW_OP_GNU_uninit:
                        break;

                    case DwarfNative.DW_OP_GNU_encoded_addr:
                        op.Operand1.U64 = ReadEncodedValue(reader, kind);
                        break;

                    case DwarfNative.DW_OP_GNU_parameter_ref:
                        op.Operand1.U64 = reader.ReadU32();
                        break;

                    default:
                        throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {kind} is not supported");
                }

                // Store the size of the current op
                op.Size = reader.Offset - op.Offset;
            }

            return exprLoc;
        }

        private static ulong ReadEncodedValue(DwarfReaderWriter reader, DwarfOperationKind kind)
        {
            var size = reader.ReadU8();
            switch (size)
            {
                case 0:
                    return reader.ReadUInt();
                case 1:
                    return reader.ReadU8();
                case 2:
                    return reader.ReadU16();
                case 4:
                    return reader.ReadU32();
                case 8:
                    return reader.ReadU64();
                default:
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        private static void ResolveAttributeReferenceWithinCompilationUnit(DwarfDIEReference dieRef, in DebugInfoReaderContext context, bool errorIfNotFound)
        {
            if (context.RegisteredDIEPerCompilationUnit.TryGetValue(dieRef.Offset, out var die))
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
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}, section 0x{(dieRef.Offset + context.OffsetOfCompilationUnitInSection):x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    context.UnresolvedDIECompilationUnitReference.Add(dieRef);
                }
            }
        }

        private static void ResolveAttributeReferenceWithinSection(DwarfDIEReference dieRef, in DebugInfoReaderContext context, bool errorIfNotFound)
        {
            if (context.RegisteredDIEPerSection.TryGetValue(dieRef.Offset, out var die))
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
                        context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    context.AttributesWithUnresolvedDIESectionReference.Add(dieRef);
                }
            }
        }
        
        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

        internal void Write(DwarfReaderWriter writer)
        {
            
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

        private struct DebugInfoReaderContext
        {
            public static DebugInfoReaderContext Create(int lineCount)
            {
                return new DebugInfoReaderContext()
                {
                    Abbreviations = new Dictionary<ulong, DwarfAbbreviation>(),
                    RegisteredDIEPerCompilationUnit = new Dictionary<ulong, DwarfDIE>(),
                    RegisteredDIEPerSection = new Dictionary<ulong, DwarfDIE>(),
                    UnresolvedDIECompilationUnitReference = new List<DwarfDIEReference>(),
                    AttributesWithUnresolvedDIESectionReference = new List<DwarfDIEReference>(),
                    OffsetToDebugLine = new Dictionary<ulong, DwarfDebugLine>(lineCount)
                };
            }

            public Dictionary<ulong, DwarfAbbreviation> Abbreviations;

            public Dictionary<ulong, DwarfDIE> RegisteredDIEPerCompilationUnit;

            public Dictionary<ulong, DwarfDIE> RegisteredDIEPerSection;

            public List<DwarfDIEReference> UnresolvedDIECompilationUnitReference;

            public List<DwarfDIEReference> AttributesWithUnresolvedDIESectionReference;

            public Dictionary<ulong, DwarfDebugLine> OffsetToDebugLine;

            public DiagnosticBag Diagnostics;

            public bool Is64Address;

            public ushort Version;

            public ulong OffsetOfCompilationUnitInSection;
        }
    }
}