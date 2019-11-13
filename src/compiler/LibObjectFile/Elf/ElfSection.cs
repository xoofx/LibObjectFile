using System;
using System.Diagnostics;

namespace LibObjectFile.Elf
{
    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class ElfSection : IElfSectionView
    {
        private ElfSectionType _type;

        protected ElfSection() : this(ElfSectionType.Null)
        {
        }

        protected ElfSection(ElfSectionType sectionType)
        {
            _type = sectionType;
            Alignment = 1;
        }

        public virtual ElfSectionType Type
        {
            get => _type;
            set => _type = value;
        }

        public ElfSectionFlags Flags { get; set; }

        public ElfString Name { get; set; }

        public ulong VirtualAddress { get; set; }

        public ulong Alignment { get; set; }

        public ElfSectionLink Link { get; set; }

        public ElfSectionLink Info { get; set; }
        
        public ElfObjectFile Parent { get; internal set; }

        public uint Index { get; internal set; }

        public abstract ulong Size { get; }

        public virtual ulong TableEntrySize => 0;

        public ulong OriginalSize { get; internal set; }
        
        public ulong OriginalTableEntrySize { get; internal set; }

        /// <summary>
        /// Gets the offset from the beginning of the file
        /// </summary>
        public ulong Offset { get; internal set; }

        internal void WriteInternal(ElfWriter writer)
        {
            Write(writer);
        }

        internal void ReadInternal(ElfReader reader)
        {
            Read(reader);
        }

        protected abstract void Read(ElfReader reader);

        protected abstract void Write(ElfWriter writer);

        public virtual string FullName => Name;

        public virtual void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            // Verify that Link is correctly setup for this section
            switch (Type)
            {
                case ElfSectionType.DynamicLinking:
                case ElfSectionType.DynamicLinkerSymbolTable:
                case ElfSectionType.SymbolTable:
                    Link.TryGetSectionSafe<ElfStringTable>(ElfSectionType.StringTable, this.GetType().Name, nameof(Link), this, diagnostics, out _);
                    break;
                case ElfSectionType.SymbolHashTable:
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    Link.TryGetSectionSafe<ElfSymbolTable>(ElfSectionType.SymbolTable, this.GetType().Name, nameof(Link), this, diagnostics, out _);
                    break;
            }
        }

        protected virtual void AfterRead(ElfReader reader)
        {
        }

        protected virtual void BeforeWrite(ElfWriter writer)
        {
        }

        public override string ToString()
        {
            return $"Section [{Index}] `{FullName}` ";
        }

        internal void BeforeWriteInternal(ElfWriter writer)
        {
            BeforeWrite(writer);
        }

        internal void AfterReadInternal(ElfReader reader)
        {
            AfterRead(reader);
        }
    }
}