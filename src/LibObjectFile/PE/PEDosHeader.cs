// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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
}