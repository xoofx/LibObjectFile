// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Operations.Count,nq}")]
    public class DwarfExpression : DwarfContainer
    {
        private readonly List<DwarfOperation> _operations;

        public DwarfExpression()
        {
            _operations = new List<DwarfOperation>();
        }

        public IReadOnlyList<DwarfOperation> Operations => _operations;

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

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}