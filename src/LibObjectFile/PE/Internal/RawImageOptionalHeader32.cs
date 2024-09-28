// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeader32
{
    public RawImageOptionalHeaderCommonPart1 Common;
    public RawImageOptionalHeaderBase32 Base32;
    public RawImageOptionalHeaderCommonPart2 Common2;
    public RawImageOptionalHeaderSize32 Size32;
    public RawImageOptionalHeaderCommonPart3 Common3;
    
    // In case of a PE Header with zero directories
    public static unsafe int MinimumSize => sizeof(RawImageOptionalHeader32) - sizeof(ImageDataDirectoryArray);
}

