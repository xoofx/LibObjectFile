namespace LibObjectFile.Elf
{
    public enum ElfSegmentTypeCore : uint
    {
        /// <summary>
        /// Program header table entry unused
        /// </summary>
        Null = RawElf.PT_NULL,

        /// <summary>
        /// Loadable program segment
        /// </summary>
        Load = RawElf.PT_LOAD,

        /// <summary>
        /// Dynamic linking information
        /// </summary>
        Dynamic = RawElf.PT_DYNAMIC,

        /// <summary>
        /// Program interpreter
        /// </summary>
        Interpreter = RawElf.PT_INTERP,

        /// <summary>
        /// Auxiliary information
        /// </summary>
        Note = RawElf.PT_NOTE,

        /// <summary>
        /// Reserved
        /// </summary>
        SectionHeaderLib = RawElf.PT_SHLIB,

        /// <summary>
        /// Entry for header table itself
        /// </summary>
        ProgramHeader = RawElf.PT_PHDR,

        /// <summary>
        /// Thread-local storage segment
        /// </summary>
        Tls = RawElf.PT_TLS,
    }
}