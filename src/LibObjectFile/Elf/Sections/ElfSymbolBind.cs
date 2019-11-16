namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a symbol binding.
    /// </summary>
    public enum ElfSymbolBind : byte
    {
        /// <summary>
        /// Local symbol
        /// </summary>
        Local = RawElf.STB_LOCAL,
        
        /// <summary>
        /// Global symbol
        /// </summary>
        Global = RawElf.STB_GLOBAL,

        /// <summary>
        /// Weak symbol
        /// </summary>
        Weak = RawElf.STB_WEAK,

        /// <summary>
        /// Unique symbol
        /// </summary>
        GnuUnique = RawElf.STB_GNU_UNIQUE,

        /// <summary>
        /// OS-specific 0
        /// </summary>
        SpecificOS0 = RawElf.STB_GNU_UNIQUE,

        /// <summary>
        /// OS-specific 1
        /// </summary>
        SpecificOS1 = RawElf.STB_GNU_UNIQUE + 1,

        /// <summary>
        /// OS-specific 2
        /// </summary>
        SpecificOS2 = RawElf.STB_GNU_UNIQUE + 2,

        /// <summary>
        /// Processor-specific 0
        /// </summary>
        SpecificProcessor0 = RawElf.STB_LOPROC,

        /// <summary>
        /// Processor-specific 1
        /// </summary>
        SpecificProcessor1 = RawElf.STB_LOPROC + 1,

        /// <summary>
        /// Processor-specific 2
        /// </summary>
        SpecificProcessor2 = RawElf.STB_LOPROC + 2,
    }
}