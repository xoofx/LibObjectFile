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