// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfExpressionLocation : ObjectFileNode
    {
        public Stream Stream { get; set; }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }

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
            
            bool result = true;
            while (true)
            {
                if (reader.Offset == reader.Length)
                {
                    break;
                }
                // 7.5 Format of Debugging Information
                // - Each such contribution consists of a compilation unit header

                var startOffset = reader.Offset;
                if (!TryReadCompilationUnitHeader(reader, out var header, out var offsetEndOfUnit))
                {
                    reader.Offset = offsetEndOfUnit;
                    result = false;
                    continue;
                }

                var cu = new DwarfCompilationUnit
                {
                    Offset = (ulong)startOffset,
                    Is64 = reader.Is64BitDwarfFormat,
                    Version = header.version,
                    AddressSize = header.address_size
                };

                internalContext.OffsetOfCompilationUnitInSection = startOffset;
                internalContext.RegisteredDIEPerCompilationUnit.Clear();
                internalContext.AttributesWithUnresolvedDIECompilationUnitReference.Clear();
                
                internalContext.AddressSize = header.address_size;

                var abbreviation = Parent.DebugAbbrevTable.Read(reader.FileContext.DebugAbbrevStream, header.debug_abbrev_offset);
                
                // Each debugging information entry begins with an unsigned LEB128 number containing the abbreviation code for the entry.
                cu.Root = ReadDIE(reader, ref internalContext, abbreviation, 0);

                // Resolve attribute reference within the CU
                foreach (var unresolvedAttrRef in internalContext.AttributesWithUnresolvedDIECompilationUnitReference)
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

        private DwarfDIE ReadDIE(DwarfReaderWriter reader, ref DebugInfoReaderContext internalContext, DwarfAbbreviation abbreviation, int level)
        {
            var startDIEOffset = reader.Offset;
            var abbreviationCode = reader.ReadLEB128();

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
                header.debug_abbrev_offset = reader.ReadNativeUInt();

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
                header.debug_abbrev_offset = reader.ReadNativeUInt();

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
                        attr.ValueAsU64 = context.AddressSize == 8 ? reader.ReadU64() : reader.ReadU32();
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
                        var length = reader.ReadLEB128();
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
                case DwarfNative.DW_FORM_data1:
                    {
                        attr.ValueAsU64 = reader.ReadU8();
                        break;
                    }
                case DwarfNative.DW_FORM_flag:
                    {
                        attr.ValueAsBoolean = reader.ReadU8() != 0;
                        break;
                    }
                case DwarfNative.DW_FORM_sdata:
                    {
                        attr.ValueAsI64 = reader.ReadSignedLEB128();
                        break;
                    }
                case DwarfNative.DW_FORM_strp:
                    {
                        var offset = reader.ReadNativeUInt();
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
                        attr.ValueAsU64 = reader.ReadLEB128();
                        break;
                    }
                case DwarfNative.DW_FORM_ref_addr:
                    {
                        attr.ValueAsU64 = reader.ReadNativeUInt();
                        ResolveAttributeReferenceWithinSection(attr, context, false);
                        break;
                    }
                case DwarfNative.DW_FORM_ref1:
                    {
                        attr.ValueAsU64 = reader.ReadU8();
                        ResolveAttributeReferenceWithinCompilationUnit(attr, context, false);
                        break;
                    }
                case DwarfNative.DW_FORM_ref2:
                    {
                        attr.ValueAsU64 = reader.ReadU16();
                        ResolveAttributeReferenceWithinCompilationUnit(attr, context, false);
                        break;
                    }
                case DwarfNative.DW_FORM_ref4:
                    {
                        attr.ValueAsU64 = reader.ReadU32();
                        ResolveAttributeReferenceWithinCompilationUnit(attr, context, false);
                        break;
                    }
                case DwarfNative.DW_FORM_ref8:
                    {
                        attr.ValueAsU64 = reader.ReadU64();
                        ResolveAttributeReferenceWithinCompilationUnit(attr, context, false);
                        break;
                    }
                case DwarfNative.DW_FORM_ref_udata:
                    {
                        attr.ValueAsU64 = reader.ReadLEB128();
                        ResolveAttributeReferenceWithinCompilationUnit(attr, context, false);
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
                        attr.ValueAsU64 = reader.ReadNativeUInt();
                        //Console.WriteLine($"attribute {attr.Key} offset: {attr.ValueAsU64}");
                        break;
                    }
                case DwarfNative.DW_FORM_exprloc:
                    {
                        var length = reader.ReadLEB128();
                        var offset = reader.Offset;
                        var stream = reader.ReadAsStream(length);
                        var expressionLocation = new DwarfExpressionLocation()
                        {
                            Offset = offset,
                            Size = length,
                            Stream = stream
                        };
                        attr.ValueAsObject = expressionLocation;
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

        private static void ResolveAttributeReferenceWithinCompilationUnit(DwarfAttribute attr, in DebugInfoReaderContext context, bool errorIfNotFound)
        {
            if (context.RegisteredDIEPerCompilationUnit.TryGetValue(attr.ValueAsU64, out var die))
            {
                attr.ValueAsU64 = 0;
                attr.ValueAsObject = die;
            }
            else
            {
                if (errorIfNotFound)
                {
                    context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{attr.ValueAsI64:x}, section 0x{(attr.ValueAsU64 + context.OffsetOfCompilationUnitInSection):x}) for attribute {attr.Key} at offset 0x{attr.Offset:x}");
                }
                else
                {
                    context.AttributesWithUnresolvedDIECompilationUnitReference.Add(attr);
                }
            }
        }

        private static void ResolveAttributeReferenceWithinSection(DwarfAttribute attr, in DebugInfoReaderContext context, bool errorIfNotFound)
        {
            if (context.RegisteredDIEPerSection.TryGetValue(attr.ValueAsU64, out var die))
            {
                attr.ValueAsU64 = 0;
                attr.ValueAsObject = die;
            }
            else
            {
                if (errorIfNotFound)
                {
                    context.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{attr.ValueAsI64:x}) for attribute {attr.Key} at offset 0x{attr.Offset:x}");
                }
                else
                {
                    context.AttributesWithUnresolvedDIESectionReference.Add(attr);
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

        private struct DebugInfoReaderContext
        {
            public static DebugInfoReaderContext Create(int lineCount)
            {
                return new DebugInfoReaderContext()
                {
                    RegisteredDIEPerCompilationUnit = new Dictionary<ulong, DwarfDIE>(),
                    RegisteredDIEPerSection = new Dictionary<ulong, DwarfDIE>(),
                    AttributesWithUnresolvedDIECompilationUnitReference = new List<DwarfAttribute>(),
                    AttributesWithUnresolvedDIESectionReference = new List<DwarfAttribute>(),
                    OffsetToDebugLine = new Dictionary<ulong, DwarfDebugLine>(lineCount)
                };
            }

            public Dictionary<ulong, DwarfDIE> RegisteredDIEPerCompilationUnit;

            public Dictionary<ulong, DwarfDIE> RegisteredDIEPerSection;

            public List<DwarfAttribute> AttributesWithUnresolvedDIECompilationUnitReference;

            public List<DwarfAttribute> AttributesWithUnresolvedDIESectionReference;

            public Dictionary<ulong, DwarfDebugLine> OffsetToDebugLine;

            public DiagnosticBag Diagnostics;

            public uint AddressSize;

            public ulong OffsetOfCompilationUnitInSection;
        }
    }
}