namespace LibObjectFile.Elf
{
    public enum ElfSectionType
    {
        /// <summary>
        /// Section header table entry unused
        /// </summary>
        Null = 0,

        /// <summary>
        /// Program data
        /// </summary>
        ProgBits,

        /// <summary>
        /// Symbol table
        /// </summary>
        SymbolTable,

        /// <summary>
        /// String table
        /// </summary>
        StringTable,

        /// <summary>
        /// Relocation entries with addends
        /// </summary>
        RelocationAddends,

        /// <summary>
        /// Symbol hash table
        /// </summary>
        SymbolHashTable,

        /// <summary>
        /// Dynamic linking information 
        /// </summary>
        DynamicLinking,

        /// <summary>
        /// Notes
        /// </summary>
        Note,

        /// <summary>
        /// Program space with no data (bss)
        /// </summary>
        NoBits,

        /// <summary>
        /// Relocation entries, no addends
        /// </summary>
        Relocation,

        /// <summary>
        /// Reserved
        /// </summary>
        Shlib,

        /// <summary>
        /// Dynamic linker symbol table
        /// </summary>
        DynamicLinkerSymbolTable,
    }
}