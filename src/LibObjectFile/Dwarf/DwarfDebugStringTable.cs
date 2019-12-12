// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfDebugStringTable : DwarfSection
    {
        private readonly Dictionary<string, ulong> _stringToOffset;
        private readonly Dictionary<ulong, string> _offsetToString;

        public DwarfDebugStringTable()
        {
            _stringToOffset = new Dictionary<string, ulong>();
            _offsetToString = new Dictionary<ulong, string>();
        }

        public DwarfDebugStringTable(Stream stream) : this()
        {
            Stream = stream;
        }

        public Stream Stream { get; set; }

        public string GetStringFromOffset(ulong offset)
        {
            if (_offsetToString.TryGetValue(offset, out var text))
            {
                return text;
            }

            Stream.Position = (long) offset;
            text = Stream.ReadStringUTF8NullTerminated();
            _offsetToString[offset] = text;
            _stringToOffset[text] = offset;
            return text;
        }

        public bool ContainsString(string text)
        {
            if (text == null) return false;
            return _stringToOffset.ContainsKey(text);
        }

        public ulong GetOrCreateString(string text)
        {
            if (text == null) return 0;

            ulong offset;
            if (_stringToOffset.TryGetValue(text, out offset))
            {
                return offset;
            }

            Stream.Position = Stream.Length;
            offset = (ulong)Stream.Position;
            Stream.WriteStringUTF8NullTerminated(text);
            _offsetToString[offset] = text;
            _stringToOffset[text] = offset;
            return offset;
        }

        internal void Read(DwarfReaderWriter reader)
        {
            if (reader.Context.DebugStringStream.Stream == null)
            {
                return;
            }

            var previousStream = reader.Stream;
            try
            {
                reader.Stream = reader.Context.DebugStringStream;
                Stream = reader.ReadAsStream((ulong) reader.Stream.Length);
            }
            finally
            {
                reader.Stream = previousStream;
            }
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

        public void Write(DwarfWriter writer)
        {
        }
    }
}