// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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