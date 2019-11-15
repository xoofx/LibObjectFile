using System;

namespace LibObjectFile.Elf
{
    public abstract class ElfObjectFilePart
    {
        private ulong _offset;
        private ulong _size;

        public virtual ulong Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public virtual ElfValueKind OffsetKind { get; set; }
        
        public ElfObjectFile Parent { get; internal set; }
        
        public uint Index { get; internal set; }

        public virtual ulong Size
        {
            get => SizeKind == ElfValueKind.Auto ? GetSize() : _size;
            set
            {
                SizeKind = ElfValueKind.Absolute;
                _size = value;
            }
        }
       
        public virtual ElfValueKind SizeKind { get; set; }

        protected virtual ulong GetSize() => 0;


        public bool Contains(ulong offset)
        {
            return offset >= _offset && offset < _offset + Size;
        }

        public bool Contains(ElfObjectFilePart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return Contains(part.Offset) || part.Size != 0 && Contains(part.Offset + part.Size - 1);
        }

        public virtual void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }
    }
}