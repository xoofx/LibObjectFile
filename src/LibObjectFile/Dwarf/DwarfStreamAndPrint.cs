// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Dwarf
{
    public struct DwarfStreamAndPrint
    {
        public DwarfStreamAndPrint(Stream stream) : this()
        {
            Stream = stream;
        }

        public DwarfStreamAndPrint(Stream stream, TextWriter printer)
        {
            Stream = stream;
            Printer = printer;
        }

        public Stream Stream { get; set; }

        public TextWriter Printer { get; set; }

        public static implicit operator DwarfStreamAndPrint(Stream stream)
        {
            return new DwarfStreamAndPrint(stream);
        }

        public static implicit operator Stream(DwarfStreamAndPrint streamAndPrint)
        {
            return streamAndPrint.Stream;
        }
    }
}