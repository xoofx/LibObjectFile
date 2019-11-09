namespace LibObjectFile.Elf
{
    public enum ElfObjectFileType
    {
        /// <summary>
        /// No file type
        /// </summary>
        None,

        /// <summary>
        /// Relocatable file
        /// </summary>
        Relocatable,

        /// <summary>
        /// Executable file
        /// </summary>
        Executable,

        /// <summary>
        /// Shared object file 
        /// </summary>
        Dynamic,

        /// <summary>
        /// Core file
        /// </summary>
        Core,
    }
}