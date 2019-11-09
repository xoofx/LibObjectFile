using System;
using System.Collections.Generic;
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
        }

        public override ulong Size => (uint)_table.Position;

        public override void Write(Stream stream)
        {
            stream.Write(_table.GetBuffer(), 0, (int)Size);
        }

        public uint GetOrCreateIndex(string text)
        {
            if (_map.TryGetValue(text, out uint index))
            {
                return index;
            }

            index = (uint)Size;
            _map.Add(text, index);
            var reservedBytes = Encoding.UTF8.GetByteCount(text) + 1;
            Span<byte> span = stackalloc byte[reservedBytes];
            Encoding.UTF8.GetEncoder().GetBytes(text, span, true);
            span[reservedBytes - 1] = 0;
            _table.Write(span);
            return index;
        }

        public void Reset()
        {
            _table.SetLength(0);
            _map.Clear();
        }
    }
}