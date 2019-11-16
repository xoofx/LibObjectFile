// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Utils
{
    public struct BinaryEncoderDirect : IBinaryEncoder
    {
        public short Encode(short value) => value;

        public ushort Encode(ushort value) => value;

        public int Encode(int value) => value;

        public uint Encode(uint value) => value;

        public long Encode(long value) => value;

        public ulong Encode(ulong value) => value;
    }
}