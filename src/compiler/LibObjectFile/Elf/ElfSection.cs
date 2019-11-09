using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public abstract class ElfSection
    {
        private ElfSectionSpecialType _specialType;

        protected ElfSection()
        {
            Alignment = 1;
        }

        public ElfSectionSpecialType SpecialType { get => _specialType;
            set => _specialType = value;
        }

        public ElfSectionType Type { get; set; }

        public ElfSectionFlags Flags { get; set; }

        public string Name { get; set; }

        public abstract ulong Size { get; }

        public ulong VirtualAddress { get; set; }

        public ulong Alignment { get; set; }

        public virtual ulong FixedEntrySize => 0;

        public ElfSection Link { get; set; }

        public ElfSection Info { get; set; }

        public ElfObjectFile Parent { get; internal set; }

        public uint Index { get; internal set; }

        /// <summary>
        /// Gets or sets the offset from the beginning of the file
        /// </summary>
        internal ulong Offset { get; set; }
        
        /// <summary>
        /// Gets or sets the index in the string name sections
        /// </summary>
        internal uint NameStringIndex { get; set; }

        public abstract void Write(Stream stream);
    }
}