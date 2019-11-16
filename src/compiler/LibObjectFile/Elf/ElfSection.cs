using System;
using System.Diagnostics;

namespace LibObjectFile.Elf
{
    // TODO: SPECS ELF: Executable and Linkable Format
    // TODO: Every section in an object file has exactly one section header describing it. Section headers may exist that do not have a section.
    // TODO: Sections in a file may not overlap. No byte in a file resides in more than one section.
    // TODO: Each section occupies one contiguous (possibly empty) sequence of bytes within a file.

    /// <summary>
    /// 
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class ElfSection : ElfObjectFilePart
    {
        private ElfSectionType _type;

        protected ElfSection() : this(ElfSectionType.Null)
        {
        }

        protected ElfSection(ElfSectionType sectionType)
        {
            _type = sectionType;
        }

        public virtual ElfSectionType Type
        {
            get => _type;
            set => _type = value;
        }

        public virtual ElfSectionFlags Flags { get; set; }

        public virtual ElfString Name { get; set; }

        public virtual ulong VirtualAddress { get; set; }

        public virtual ulong Alignment { get; set; }

        public virtual ElfSectionLink Link { get; set; }

        public virtual ElfSectionLink Info { get; set; }


        // TODO: The member contains 0 if the section does not hold a table of fixed-size entries.

        public virtual ulong TableEntrySize => 0;

        public uint SectionIndex { get; internal set; }

        public ulong OriginalTableEntrySize { get; internal set; }

        public bool IsShadow => this is ElfShadowSection;

        public virtual bool HasContent => Type != ElfSectionType.NoBits;

        internal void WriteInternal(ElfWriter writer)
        {
            Write(writer);
        }

        internal void ReadInternal(ElfReader reader)
        {
            Read(reader);
            // After reading the size must be Auto by default
            SizeKind = ElfValueKind.Auto;
        }

        protected abstract void Read(ElfReader reader);

        protected abstract void Write(ElfWriter writer);

        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            if (Type != ElfSectionType.Null && Type != ElfSectionType.NoBits && SizeKind != ElfValueKind.Auto)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionSizeKind, $"Invalid {nameof(SizeKind)}: {SizeKind} for `{this}`. Expecting {ElfValueKind.Absolute}.");
            }

            // Check parent for link section
            if (Link.Section != null)
            {
                if (Link.Section.Parent != this.Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionLinkParent, $"Invalid parent for {nameof(Link)}: `{Link}` used by section `{this}`. The {nameof(Link)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfObjectFile)} than this section");
                }
            }

            if (Info.Section != null)
            {
                if (Info.Section.Parent != this.Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionInfoParent, $"Invalid parent for {nameof(Info)}: `{Info}` used by section `{this}`. The {nameof(Info)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfObjectFile)} than this section");
                }
            }

            // Verify that Link is correctly setup for this section
            switch (Type)
            {
                case ElfSectionType.DynamicLinking:
                case ElfSectionType.DynamicLinkerSymbolTable:
                case ElfSectionType.SymbolTable:
                    Link.TryGetSectionSafe<ElfStringTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.StringTable);
                    break;
                case ElfSectionType.SymbolHashTable:
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    Link.TryGetSectionSafe<ElfSymbolTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);
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
            return $"Section [{SectionIndex}](Internal: {Index}) `{Name}` ";
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