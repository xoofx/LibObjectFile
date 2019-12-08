// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public struct DwarfConstant
    {
        public DwarfConstant(int value)
        {
            AsValue = new DwarfInteger() {I64 = value};
            AsExpression = null;
        }

        public DwarfInteger AsValue;

        public DwarfExpression AsExpression;


        public override string ToString()
        {
            if (AsExpression != null) return $"Constant Expression: {AsExpression}";
            return $"Constant Value: {AsValue}";
        }

        public static implicit operator DwarfConstant(int value)
        {
            return new DwarfConstant(value);
        }
    }
}