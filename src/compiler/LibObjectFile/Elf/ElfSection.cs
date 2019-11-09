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
            if (Parent == null) throw new InvalidOperationException($"Cannot write this section instance `{this}` without being attached to a `{nameof(ElfObjectFile)}`");
            Write(writer);
        }

        protected abstract void Write(ElfWriter writer);

        internal void PrepareWriteInternal(ElfWriter writer)
        {
            if (Parent == null) throw new InvalidOperationException($"Cannot prepare for write this section instance `{this}` without being attached to a `{nameof(ElfObjectFile)}`");
            PrepareWrite(writer);
        }
        
        
        protected virtual void PrepareWrite(ElfWriter writer)
        {
        }
        
        internal uint GetInfoIndexInternal(ElfWriter writer)
        {
            if (Parent == null) throw new InvalidOperationException($"Cannot prepare for write this section instance `{this}` without being attached to a `{nameof(ElfObjectFile)}`");
            return GetInfoIndex(writer);
        }
        
        protected virtual uint GetInfoIndex(ElfWriter writer)
        {
            return 0;
        }

        public override string ToString()
        {
            return $"Section [{Index}] `{Name}` ";
        }
    }
}