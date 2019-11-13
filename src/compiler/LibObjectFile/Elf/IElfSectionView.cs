namespace LibObjectFile.Elf
{
    public interface IElfSectionView
    {
        ElfSectionType Type { get; }

        ElfSectionFlags Flags { get; }

        ElfString Name { get;  }

        ulong VirtualAddress { get; }

        ulong Alignment { get; }

        ElfSectionLink Link { get; }

        ElfObjectFile Parent { get; }

        uint Index { get; }

        ulong Size { get; }

        ulong TableEntrySize { get; }

        ElfSectionLink Info { get; }
    }
}