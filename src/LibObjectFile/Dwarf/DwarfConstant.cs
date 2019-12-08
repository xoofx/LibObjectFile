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
            AsObject = null;
        }

        public DwarfConstant(DwarfExpression expression)
        {
            AsValue = default;
            AsObject = expression;
        }

        public DwarfConstant(DwarfDIE dieRef)
        {
            AsValue = default;
            AsObject = dieRef;
        }
        
        public DwarfInteger AsValue;

        public object AsObject;

        public DwarfExpression AsExpression => AsObject as DwarfExpression;

        public DwarfDIE AsReference => AsObject as DwarfDIE;
        
        public override string ToString()
        {
            if (AsExpression != null) return $"Constant Expression: {AsExpression}";
            if (AsReference != null) return $"Constant Reference: {AsReference}";
            return $"Constant Value: {AsValue}";
        }

        public static implicit operator DwarfConstant(int value)
        {
            return new DwarfConstant(value);
        }

        public static implicit operator DwarfConstant(DwarfExpression value)
        {
            return new DwarfConstant(value);
        }
        
        public static implicit operator DwarfConstant(DwarfDIE value)
        {
            return new DwarfConstant(value);
        }
    }
}