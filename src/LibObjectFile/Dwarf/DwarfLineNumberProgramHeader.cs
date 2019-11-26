// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfLineNumber
    {
        
    }

    struct DwarfLineNumberProgramHeader32
    {
        public ulong unit_length;
        public ushort version;

        public uint header_length;
        public byte minimum_instruction_length;
    }

    // Parsing LineNumberProgramHeader
    // https://github.com/gimli-rs/gimli/blob/master/src/read/line.rs#L1280-L1420

    // 7.5.1.1 Compilation Unit Header 


    struct DwarfLineNumberProgramHeader64
    {
        /// <summary>
        /// The size in bytes of the line number information for this compilation unit, not including the unit_length field itself
        /// </summary>
        public ulong unit_length;

        /// <summary>
        /// A version number (see Appendix F). This number is specific to the line number information and is independent of the DWARF version number.
        /// </summary>
        public ushort version;

        /// <summary>
        /// The number of bytes following the header_length field to the beginning of the first byte of the line number program itself
        /// </summary>
        public ulong header_length;

        /// <summary>
        /// The size in bytes of the smallest target machine instruction.
        /// Line number program opcodes that alter the address and op_index registers use this and maximum_operations_per_instruction in their calculations.
        /// </summary>
        public byte minimum_instruction_length;

        /// <summary>
        /// The maximum number of individual operations that may be encoded in an instruction.
        /// Line number program opcodes that alter the address and op_index registers use this and minimum_instruction_length in their calculations. 
        /// </summary>
        public byte maximum_operations_per_instruction;
        
        /// <summary>
        /// <c>true</c>
        /// </summary>
        public byte default_is_stmt;
        
        /// <summary>
        /// This parameter affects the meaning of the special opcodes.
        /// </summary>
        public byte line_base;

        /// <summary>
        /// This parameter affects the meaning of the special opcodes. 
        /// </summary>
        public byte line_range;

        /// <summary>
        /// The number assigned to the first special opcode. 
        /// </summary>
        public byte opcode_base;

        public byte standard_opcode_lengths;
    }

}