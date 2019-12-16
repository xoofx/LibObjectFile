// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Operations.Count,nq}")]
    public class DwarfExpression : DwarfObject<DwarfObject>
    {
        private readonly List<DwarfOperation> _operations;

        public DwarfExpression()
        {
            _operations = new List<DwarfOperation>();
        }

        public IReadOnlyList<DwarfOperation> Operations => _operations;

        internal List<DwarfOperation> InternalOperations => _operations;

        public void AddOperation(DwarfOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _operations.Add(this, operation);
        }

        public void RemoveOperation(DwarfOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _operations.Remove(this, operation);
        }

        public DwarfOperation RemoveOperationAt(int index)
        {
            return _operations.RemoveAt(this, index);
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            foreach (var op in _operations)
            {
                op.Verify(diagnostics);
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;
            foreach (var op in _operations)
            {
                op.Offset = endOffset;
                op.UpdateLayoutInternal(layoutContext);
                endOffset += op.Size;
            }

            Size = endOffset - Offset;

            // We need to shift the expression which is prefixed by its size encoded in LEB128
            var deltaLength = DwarfHelper.SizeOfULEB128(Size);
            foreach (var op in InternalOperations)
            {
                op.Offset += deltaLength;
            }

            Size += deltaLength;
        }

        protected override void Read(DwarfReader reader)
        {
            Offset = reader.Offset;
            var size = reader.ReadULEB128();
            var endPosition = reader.Offset + size;

            while (reader.Offset < endPosition)
            {
                var op = new DwarfOperation() {Offset = reader.Offset};
                op.ReadInternal(reader);
                AddOperation(op);
            }

            Size = reader.Offset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            Debug.Assert(Offset == writer.Offset);

            var startExpressionOffset = writer.Offset;
            writer.WriteULEB128(Size);

            foreach (var op in Operations)
            {
                op.WriteInternal(writer);
            }

            Debug.Assert(writer.Offset - startExpressionOffset == Size);
        }
    }
}