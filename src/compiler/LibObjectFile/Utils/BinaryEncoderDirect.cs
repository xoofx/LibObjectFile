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