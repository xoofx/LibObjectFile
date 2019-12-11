// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfDIEDeclaration : DwarfDIE
    {
        //  DW_AT_decl_column, DW_AT_decl_file, and DW_AT_decl_line
        public ulong? DeclColumn
        {
            get => GetAttributeValueOpt<ulong>(DwarfAttributeKind.decl_column);
            set => SetAttributeValueOpt<ulong>(DwarfAttributeKind.decl_column, value);
        }

        public DwarfDebugFileName DeclFile
        {
            get => GetAttributeValue<DwarfDebugFileName>(DwarfAttributeKind.decl_file);
            set => SetAttributeValue<DwarfDebugFileName>(DwarfAttributeKind.decl_file, value);
        }

        public ulong? DeclLine
        {
            get => GetAttributeValueOpt<ulong>(DwarfAttributeKind.decl_line);
            set => SetAttributeValueOpt<ulong>(DwarfAttributeKind.decl_line, value);
        }
    }
}