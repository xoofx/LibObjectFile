using System.Diagnostics;

namespace LibObjectFile.Elf
{
    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class ElfSection : IElfSectionView
    {
        private ElfSectionSpecialType _specialType;
        private ElfSectionType _type;

        protected ElfSection() : this(ElfSectionType.Null)
        {
        }

        protected ElfSection(ElfSectionType sectionType)
        {
            _type = sectionType;
            Alignment = 1;
        }

        public ElfSectionSpecialType SpecialType { get => _specialType;
            set => _specialType = value;
        }

        public virtual ElfSectionType Type
        {
            get => _type;
            set => _type = value;
        }

        public ElfSectionFlags Flags { get; set; }

        public string Name { get; set; }

        public ulong VirtualAddress { get; set; }

        public ulong Alignment { get; set; }

        public ElfSectionLink Link { get; set; }
        
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

        internal ulong GetSizeInternal()
        {
            return GetSize();
        }

        protected abstract ulong GetSize();

        internal ulong GetTableEntrySizeInternal()
        {
            return GetTableEntrySize();
        }

        protected virtual ulong GetTableEntrySize() => 0;
        
        internal void WriteInternal(ElfWriter writer)
        {
            Write(writer);
        }

        protected abstract void Write(ElfWriter writer);

        internal void PrepareWriteInternal(ElfWriter writer)
        {
            // Verify that Link is correctly setup for this section
            switch (Type)
            {
                case ElfSectionType.DynamicLinking:
                case ElfSectionType.DynamicLinkerSymbolTable:
                case ElfSectionType.SymbolTable:
                    Link.TryGetSectionSafe<ElfStringTable>(ElfSectionType.StringTable, this.GetType().Name, nameof(Link), this, writer.Diagnostics, out _);
                    break;
                case ElfSectionType.SymbolHashTable:
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    Link.TryGetSectionSafe<ElfSymbolTable>(ElfSectionType.SymbolTable, this.GetType().Name, nameof(Link), this, writer.Diagnostics, out _);
                    break;
            }

            PrepareWrite(writer);
        }

        public virtual string GetFullName()
        {
            return Name;
        }
        
        protected virtual void PrepareWrite(ElfWriter writer)
        {
        }
        
        internal uint GetInfoIndexInternal(ElfWriter writer)
        {
            return GetInfoIndex(writer);
        }
        
        protected virtual uint GetInfoIndex(ElfWriter writer)
        {
            return 0;
        }

        public override string ToString()
        {
            return $"Section [{Index}] `{GetFullName()}` ";
        }
    }
}