using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    public sealed class ElfObjectFile
    {
        private readonly List<ElfSection> _sections;

        public ElfObjectFile()
        {
            _sections = new List<ElfSection>();
            Class = ElfClass.Is64;
            OSAbi = ElfOSAbi.NONE;
            Encoding = ElfEncoding.Lsb;
            FileType = ElfObjectFileType.Relocatable;
            Arch = ElfArch.X86_64;
        }

        public ElfClass Class { get; set; }

        public ElfEncoding Encoding { get; set; }

        public ElfOSAbi OSAbi { get; set; }

        public ElfObjectFileType FileType { get; set; }

        public ElfArch Arch { get; set; }

        public ulong EntryPointAddress { get; set; }

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
            _sections.Add(section);
        }

        public void RemoveSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfObjectFile)} instance");
            }
            _sections.Remove(section);
            section.Parent = null;
        }
    }
}