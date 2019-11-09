using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    public sealed class ElfObjectFile
    {
        private readonly List<ElfSection> _sections;
        private const int MinSectionIndex = 2;

        public ElfObjectFile()
        {
            ProgramHeaders = new List<ElfProgramHeader>();
            _sections = new List<ElfSection>();
            FileClass = ElfFileClass.Is64;
            OSAbi = ElfOSAbi.NONE;
            Encoding = ElfEncoding.Lsb;
            FileType = ElfFileType.Relocatable;
            Arch = ElfArch.X86_64;
        }

        public ElfFileClass FileClass { get; set; }

        public ElfEncoding Encoding { get; set; }

        public ElfOSAbi OSAbi { get; set; }

        public ElfFileType FileType { get; set; }

        public ElfArch Arch { get; set; }

        public ulong EntryPointAddress { get; set; }

        public List<ElfProgramHeader> ProgramHeaders { get; }

        public IReadOnlyList<ElfSection> Sections => _sections;

        public void AddSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)(MinSectionIndex + _sections.Count);
            _sections.Add(section);
        }

        public void RemoveSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfObjectFile)} instance");
            }
            var i = _sections.IndexOf(section);
            _sections.RemoveAt(i);

            // Update indices for other sections
            for (; i < _sections.Count; i++)
            {
                _sections[i].Index = (uint)(MinSectionIndex + i);
            }

            section.Parent = null;
        }
    }
}