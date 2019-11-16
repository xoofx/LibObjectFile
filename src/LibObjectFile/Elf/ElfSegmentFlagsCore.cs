using System;

namespace LibObjectFile.Elf
{
    [Flags]
    public enum ElfSegmentFlagsCore : uint
    {
        /// <summary>
        /// Segment flags is undefined
        /// </summary>
        None = 0,

        /// <summary>
        /// Segment is executable
        /// </summary>
        Executable = RawElf.PF_X,

        /// <summary>
        /// Segment is writable
        /// </summary>
        Writable = RawElf.PF_W,

        /// <summary>
        /// Segment is readable
        /// </summary>
        Readable = RawElf.PF_R,
    }
}