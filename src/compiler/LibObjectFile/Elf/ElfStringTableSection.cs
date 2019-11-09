using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    public class ElfStringTableSection : ElfSection
    {
        private readonly MemoryStream _table;
        private readonly Dictionary<string, uint> _map;

        public ElfStringTableSection()
        {
            Type = ElfSectionType.StringTable;
            _table = new MemoryStream();
            _map = new Dictionary<string, uint>();
            // Always create an empty string
            GetOrCreateIndex(string.Empty);
        }

        public override ulong GetSize(ElfFileClass fileClass) => (uint) _table.Position;

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
                Span<byte> span = stackalloc byte[reservedBytes];
                Encoding.UTF8.GetEncoder().GetBytes(text, span, true);
                span[reservedBytes - 1] = 0;
                _table.Write(span);
            }

            return index;
        }

        public void Reset()
        {
            _table.SetLength(0);
            _map.Clear();
        }
    }
}