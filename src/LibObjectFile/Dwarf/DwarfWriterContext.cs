// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfWriterContext : DwarfReaderWriterContext
    {
        private DwarfAttributeForm _defaultAttributeFormForReference;

        public DwarfWriterContext()
        {
            DefaultAttributeFormForReference = DwarfAttributeForm.ref4;
        }

        public DwarfAttributeFormEx DefaultAttributeFormForReference
        {
            get => _defaultAttributeFormForReference;
            set
            {
                switch (value.Value)
                {
                    case DwarfAttributeForm.ref1:
                    case DwarfAttributeForm.ref2:
                    case DwarfAttributeForm.ref4:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
                _defaultAttributeFormForReference = value;
            }
        }
    }
}