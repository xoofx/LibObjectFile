namespace LibObjectFile.Elf
{
    public enum ElfFileType : ushort
    {
        /// <summary>
        /// No file type
        /// </summary>
        None = RawElf.ET_NONE,

        /// <summary>
        /// Relocatable file
        /// </summary>
        Relocatable = RawElf.ET_REL,

        /// <summary>
        /// Executable file
        /// </summary>
        Executable = RawElf.ET_EXEC,

        /// <summary>
        /// Shared object file 
        /// </summary>
        Dynamic = RawElf.ET_DYN,

        /// <summary>
        /// Core file
        /// </summary>
        Core = RawElf.ET_CORE,
    }
}