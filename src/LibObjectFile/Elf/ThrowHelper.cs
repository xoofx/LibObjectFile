using System;

namespace LibObjectFile.Elf
{
    internal static class ThrowHelper
    {
        public static InvalidOperationException InvalidEnum(object v)
        {
            return new InvalidOperationException($"Invalid Enum {v.GetType()}.{v}");
        }
    }
}