namespace LibObjectFile.Utils
{
    public interface IBinaryEncoder
    {
        short Encode(short value);

        ushort Encode(ushort value);

        int Encode(int value);

        uint Encode(uint value);

        long Encode(long value);

        ulong Encode(ulong value);
    }
}