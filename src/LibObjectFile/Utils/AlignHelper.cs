using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils
{
    public static class AlignHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AlignToUpper(ulong value, ulong align)
        {
            var nextValue = ((value + align - 1) / align) * align;
            return nextValue;
        }
    }
}