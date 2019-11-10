using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    public sealed class ElfStringTable : ElfSection
    {
        private readonly MemoryStream _table;
        private readonly Dictionary<string, uint> _map;

        public const string DefaultName = ".strtab";

        public const int DefaultCapacity = 256;

        public ElfStringTable() : this(DefaultCapacity)
        {
        }

        public ElfStringTable(int capacityInBytes) : base(ElfSectionType.StringTable)
        {
            if (capacityInBytes < 0) throw new ArgumentOutOfRangeException(nameof(capacityInBytes));
            Name = DefaultName;
            _table = new MemoryStream(capacityInBytes);
            _map = new Dictionary<string, uint>();
            // Always create an empty string
            GetOrCreateIndex(string.Empty);
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.StringTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfStringTable)}`. Only `{ElfSectionType.StringTable}` is valid");
                }
                base.Type = value;
            }
        }

        protected override ulong GetSize() => (uint) _table.Position;

        protected override void Write(ElfWriter writer)
        {
            writer.Stream.Write(_table.GetBuffer(), 0, (int)_table.Position);
        }

        public uint GetOrCreateIndex(string text)
        {
            // Same as empty string
            if (text == null) return 0;

            if (_map.TryGetValue(text, out uint index))
            {
                return index;
            }

            index = (uint) _table.Position;
            _map.Add(text, index);

            if (text.Length == 0)
            {
                Debug.Assert(index == 0);
                _table.WriteByte(0);
            }
            else
            {
                var reservedBytes = Encoding.UTF8.GetByteCount(text) + 1;
                var buffer = ArrayPool<byte>.Shared.Rent(reservedBytes);
                var span = new Span<byte>(buffer);
                Encoding.UTF8.GetEncoder().GetBytes(text, span, true);
                span[reservedBytes - 1] = 0;
                _table.Write(span);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return index;
        }

        public void Reset()
        {
            _table.SetLength(0);
            _map.Clear();

            // Always create an empty string
            GetOrCreateIndex(string.Empty);
        }
    }
}