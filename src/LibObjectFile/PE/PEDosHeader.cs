// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
/// <summary>
/// Represents the DOS header of a PE file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public unsafe struct PEDosHeader
{
    /// <summary>
    /// Magic number. (Original DOS field is `e_magic`)
    /// </summary>
    public PEDosMagic Magic;

    /// <summary>
    /// Bytes on last page of file. (Original DOS field is `e_cblp`)
    /// </summary>
    public ushort ByteCountOnLastPage;

    /// <summary>
    /// Pages in file. (Original DOS field is `e_cp`)
    /// </summary>
    public ushort PageCount;

    /// <summary>
    /// Relocations. (Original DOS field is `e_crlc`)
    /// </summary>
    public ushort RelocationCount;

    /// <summary>
    /// Size of header in paragraphs. (Original DOS field is `e_cparhdr`)
    /// </summary>
    public ushort SizeOfParagraphsHeader;

    /// <summary>
    /// Minimum extra paragraphs needed. (Original DOS field is `e_minalloc`)
    /// </summary>
    public ushort MinExtraParagraphs;

    /// <summary>
    /// Maximum extra paragraphs needed. (Original DOS field is `e_maxalloc`)
    /// </summary>
    public ushort MaxExtraParagraphs;

    /// <summary>
    /// Initial (relative) SS value. (Original DOS field is `e_ss`)
    /// </summary>
    public ushort InitialSSValue;

    /// <summary>
    /// Initial SP value. (Original DOS field is `e_sp`)
    /// </summary>
    public ushort InitialSPValue;

    /// <summary>
    /// Checksum. (Original DOS field is `e_csum`)
    /// </summary>
    public ushort Checksum;

    /// <summary>
    /// Initial IP value. (Original DOS field is `e_ip`)
    /// </summary>
    public ushort InitialIPValue;

    /// <summary>
    /// Initial (relative) CS value. (Original DOS field is `e_cs`)
    /// </summary>
    public ushort InitialCSValue;

    /// <summary>
    /// File address of relocation table. (Original DOS field is `e_lfarlc`)
    /// </summary>
    public ushort FileAddressRelocationTable;

    /// <summary>
    /// Overlay number. (Original DOS field is `e_ovno`)
    /// </summary>
    public ushort OverlayNumber;

    /// <summary>
    /// Reserved words. (Original DOS field is `e_res`)
    /// </summary>
    public fixed ushort Reserved[4];

    /// <summary>
    /// OEM identifier (for e_oeminfo). (Original DOS field is `e_oemid`)
    /// </summary>
    public ushort OEMIdentifier;

    /// <summary>
    /// OEM information; e_oemid specific. (Original DOS field is `e_oeminfo`)
    /// </summary>
    public ushort OEMInformation;

    /// <summary>
    /// Reserved words. (Original DOS field is `e_res2`)
    /// </summary>
    public fixed ushort Reserved2[10];

    internal uint _FileAddressPEHeader;

    /// <summary>
    /// File address of new exe header. (Original DOS field is `e_lfanew`)
    /// </summary>
    /// <remarks>This property is automatically calculated but can be slightly adjusted with  </remarks>
    public uint FileAddressPEHeader => _FileAddressPEHeader;

    /// <summary>
    /// A default DOS header for a PE file.
    /// </summary>
    internal static ref readonly PEDosHeader Default => ref Unsafe.As<byte, PEDosHeader>(ref  MemoryMarshal.GetReference(new ReadOnlySpan<byte>([
        0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
        0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 // Offset to PE (0x80 == sizeof(PEDosHeader) + DefaultDosStub.Length)
    ])));

    /// <summary>
    /// A default DOS stub for a PE file.
    /// </summary>
    internal static ReadOnlySpan<byte> DefaultDosStub => [
        0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
        0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72, 0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
        0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E, 0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
        0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];
}