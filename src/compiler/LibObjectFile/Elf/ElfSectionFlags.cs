using System;

namespace LibObjectFile.Elf
{
    [Flags]
    public enum ElfSectionFlags
    {
        None = 0,

        /// <summary>
        /// Writable
        /// </summary>
        Write = (1 << 0),

        /// <summary>
        /// Occupies memory during execution
        /// </summary>
        Alloc = (1 << 1),

        /// <summary>
        /// Executable
        /// </summary>
        Executable = (1 << 2),

        /// <summary>
        /// Might be merged
        /// </summary>
        Merge = (1 << 4),

        /// <summary>
        /// Contains nul-terminated strings
        /// </summary>
        Strings = (1 << 5),

        /// <summary>
        /// `sh_info' contains SHT index
        /// </summary>
        InfoLink = (1 << 6),

        /// <summary>
        /// Preserve order after combining
        /// </summary>
        LinkOrder = (1 << 7),

        /// <summary>
        /// Non-standard OS specific handling required
        /// </summary>
        OsNonConforming = (1 << 8),

        /// <summary>
        /// Section is member of a group. 
        /// </summary>
        Group = (1 << 9),

        /// <summary>
        /// Section hold thread-local data. 
        /// </summary>
        Tls = (1 << 10),

        /// <summary>
        /// Section with compressed data.
        /// </summary>
        Compressed = (1 << 11),
    }
}