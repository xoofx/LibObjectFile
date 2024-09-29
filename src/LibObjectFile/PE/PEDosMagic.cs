// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

#pragma warning disable CS0649
/// <summary>
/// Magic number for the <see cref="PEDosHeader.Magic"/>>
/// </summary>
public enum PEDosMagic : ushort
{
    /// <summary>
    /// MZ - DOS executable file signature.
    /// </summary>
    DOS = 0x5A4D,
    /// <summary>
    /// NE - OS/2 executable file signature.
    /// </summary>
    OS2 = 0x454E,
    /// <summary>
    /// LE - OS/2 LE or VXD.
    /// </summary>
    OS2OrVXD = 0x454C,
}