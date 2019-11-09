namespace LibObjectFile.Elf
{
    public enum ElfOSAbi
    {
        /// <summary>
        /// UNIX System V ABI
        /// </summary>
        Default = 0,

        /// <summary>
        /// Alias.
        /// </summary>
        UnixSystemV = Default,

        /// <summary>
        /// HP-UX
        /// </summary>
        HPUX = 1,

        /// <summary>
        /// NetBSD.
        /// </summary>
        NetBSD = 2,

        /// <summary>
        /// Object uses GNU ELF extensions.
        /// </summary>
        Gnu = 3,

        /// <summary>
        /// Compatibility alias.
        /// </summary>
        Linux = 3,

        /// <summary>
        /// Sun Solaris.
        /// </summary>
        Solaris = 6,

        /// <summary>
        /// IBM AIX.
        /// </summary>
        AIX = 7,

        /// <summary>
        /// SGI Irix.
        /// </summary>
        IRIX = 8,

        /// <summary>
        /// FreeBSD.
        /// </summary>
        FreeBSD = 9,

        /// <summary>
        /// Compaq TRU64 UNIX.
        /// </summary>
        TRU64 = 10,

        /// <summary>
        /// Novell Modesto.
        /// </summary>
        Modesto = 11,

        /// <summary>
        /// OpenBSD.
        /// </summary>
        OpenBSD = 12,

        /// <summary>
        /// ARM EABI
        /// </summary>
        ARMEABI = 64,

        /// <summary>
        /// ARM
        /// </summary>
        ARM = 97,

        /// <summary>
        /// Standalone (embedded) application
        /// </summary>
        Standalone = 255,
    }
}