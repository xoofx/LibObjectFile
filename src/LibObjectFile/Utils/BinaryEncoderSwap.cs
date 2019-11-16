// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Utils
{
    public struct BinaryEncoderSwap : IBinaryEncoder
    {
        public short Encode(short value) => BinaryUtil.SwapBits(value);

        public ushort Encode(ushort value) => BinaryUtil.SwapBits(value);

        public int Encode(int value) => BinaryUtil.SwapBits(value);

        public uint Encode(uint value) => BinaryUtil.SwapBits(value);

        public long Encode(long value) => BinaryUtil.SwapBits(value);

        public ulong Encode(ulong value) => BinaryUtil.SwapBits(value);
    }
}