﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Globalization;

namespace LibObjectFile.Dwarf
{
    public class DwarfLine : DwarfObject<DwarfLineSequence>
    {
        public DwarfLine()
        {
            IsStatement = true;
        }

        // -----------------------
        // DWARF 2
        // -----------------------

        /// <summary>
        /// The program-counter value corresponding to a machine instruction generated by the compiler.
        /// </summary>
        public ulong Address { get; set; }
        
        /// <summary>
        /// An unsigned integer representing the index of an operation within a VLIW instruction.
        /// The index of the first operation is 0. For non-VLIW architectures, this register will always be 0. 
        /// </summary>
        public byte OperationIndex { get; set; }

        /// <summary>
        /// The identity of the source file corresponding to a machine instruction.
        /// </summary>
        public DwarfFileName? File { get; set; }

        /// <summary>
        /// An unsigned integer indicating a source line number.
        /// Lines are numbered beginning at 1.
        /// The compiler may emit the value 0 in cases where an instruction cannot be attributed to any source line.
        /// </summary>
        public uint Line { get; set; }

        /// <summary>
        /// An unsigned integer indicating a column number within a source line.
        /// Columns are numbered beginning at 1. The value 0 is reserved to indicate that a statement begins at the “left edge” of the line.
        /// </summary>
        public uint Column { get; set; }

        /// <summary>
        /// A boolean indicating that the current instruction is a recommended breakpoint location.
        /// A recommended breakpoint location is intended to “represent” a line, a statement and/or a semantically distinct subpart of a statement. 
        /// </summary>
        public bool IsStatement { get; set; }

        /// <summary>
        /// A boolean indicating that the current instruction is the beginning of a basic block. 
        /// </summary>
        public bool IsBasicBlock { get; set; }

        // -----------------------
        // DWARF 3
        // -----------------------

        /// <summary>
        /// A boolean indicating that the current address is one (of possibly many) where execution should be suspended for an entry breakpoint of a function.
        /// </summary>
        public bool IsPrologueEnd { get; set; }

        /// <summary>
        /// A boolean indicating that the current address is one (of possibly many) where execution should be suspended for an exit breakpoint of a function.
        /// </summary>
        public bool IsEpilogueBegin { get; set; }

        /// <summary>
        /// An unsigned integer whose value encodes the applicable instruction set architecture for the current instruction. 
        /// </summary>
        public ulong Isa { get; set; }

        // -----------------------
        // DWARF 4
        // -----------------------

        /// <summary>
        /// An unsigned integer identifying the block to which the current instruction belongs.
        /// Discriminator values are assigned arbitrarily by the DWARF producer and serve to distinguish among multiple blocks that may all be
        /// associated with the same source file, line, and column.
        /// Where only one block exists for a given source position, the discriminator value should be zero.
        /// </summary>
        public ulong Discriminator { get; set; }

        public DwarfLine Clone()
        {
            return (DwarfLine) MemberwiseClone();
        }

        internal void Delta(DwarfLine against, out ulong deltaAddress,
            out byte deltaOperationIndex,
            out bool fileNameChanged,
            out int deltaLine,
            out int deltaColumn,
            out bool isStatementChanged,
            out bool isBasicBlockChanged,
            out bool isPrologueEndChanged,
            out bool isEpilogueBeginChanged,
            out bool isaChanged,
            out bool isDiscriminatorChanged)
        {
            deltaAddress = against.Address - this.Address;
            deltaOperationIndex = (byte)(against.OperationIndex - this.OperationIndex);
            fileNameChanged = !ReferenceEquals(this.File, against.File);
            deltaLine = (int) ((long) against.Line - (long) this.Line);
            deltaColumn = (int) ((long) against.Column - (long) this.Column);
            isStatementChanged = against.IsStatement != this.IsStatement;
            isBasicBlockChanged = against.IsBasicBlock != this.IsBasicBlock;
            isPrologueEndChanged = against.IsPrologueEnd != this.IsPrologueEnd;
            isEpilogueBeginChanged = against.IsEpilogueBegin != this.IsEpilogueBegin;
            isaChanged = against.Isa != this.Isa;
            isDiscriminatorChanged = against.Discriminator != this.Discriminator;
        }

        internal void Reset(DwarfFileName? firstFile, bool isStatement)
        {
            Address = 0;
            File = firstFile;
            Line = 1;
            Column = 0;
            this.IsStatement = isStatement;
            IsBasicBlock = false;

            // DWARF 3
            IsPrologueEnd = false;
            IsEpilogueBegin = false;
            Isa = 0;

            // DWARF 5
            Discriminator = 0;
        }

        internal void SpecialReset()
        {
            IsBasicBlock = false;
            IsPrologueEnd = false;
            IsEpilogueBegin = false;
            Discriminator = 0;
        }

        internal DwarfLineState ToState()
        {
            return new DwarfLineState()
            {
                Address = Address,
                Column = Column,
                Discriminator = Discriminator,
                File = File,
                Isa = Isa,
                IsBasicBlock = IsBasicBlock,
                IsEpilogueBegin = IsEpilogueBegin,
                IsPrologueEnd = IsPrologueEnd,
                IsStatement = IsStatement,
                OperationIndex = OperationIndex,
                Line = Line,
            };
        }
        
        public override string ToString()
        {
            return $"{nameof(Position)}: 0x{Position:x8}, {nameof(Address)}: 0x{Address:x16}, {nameof(File)}: {File}, {nameof(Line)}: {Line,4}, {nameof(Column)}: {Column,2}, {nameof(IsStatement)}: {Bool2Str(IsStatement),5}, {nameof(IsBasicBlock)}: {Bool2Str(IsBasicBlock),5}, {nameof(IsPrologueEnd)}: {Bool2Str(IsPrologueEnd),5}, {nameof(IsEpilogueBegin)}: {Bool2Str(IsEpilogueBegin),5}, {nameof(Isa)}: {Isa,3}, {nameof(Discriminator)}: {Discriminator,3}";
        }

        private static string Bool2Str(bool value) => value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
        }

        protected override void Read(DwarfReader reader)
        {
        }
        
        protected override void Write(DwarfWriter writer)
        {
        }
    }
}