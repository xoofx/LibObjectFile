// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawImageOptionalHeaderCommonPart2
{
    /// <summary>
    /// The alignment of sections in memory, in bytes.
    /// </summary>
    public uint SectionAlignment;

    /// <summary>
    /// The alignment of the raw data of sections in the image file, in bytes.
    /// </summary>
    public uint FileAlignment;

    /// <summary>
    /// The major version number of the required operating system.
    /// </summary>
    public ushort MajorOperatingSystemVersion;

    /// <summary>
    /// The minor version number of the required operating system.
    /// </summary>
    public ushort MinorOperatingSystemVersion;

    /// <summary>
    /// The major version number of the image.
    /// </summary>
    public ushort MajorImageVersion;

    /// <summary>
    /// The minor version number of the image.
    /// </summary>
    public ushort MinorImageVersion;

    /// <summary>
    /// The major version number of the subsystem.
    /// </summary>
    public ushort MajorSubsystemVersion;

    /// <summary>
    /// The minor version number of the subsystem.
    /// </summary>
    public ushort MinorSubsystemVersion;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Win32VersionValue;

    /// <summary>
    /// The size of the image, including all headers, as loaded in memory, in bytes. Must be a multiple of SectionAlignment.
    /// </summary>
    public uint SizeOfImage;

    /// <summary>
    /// The combined size of all headers (DOS, PE, section headers), rounded up to a multiple of FileAlignment.
    /// </summary>
    public uint SizeOfHeaders;

    /// <summary>
    /// The image file checksum.
    /// </summary>
    public uint CheckSum;

    /// <summary>
    /// The subsystem required to run this image.
    /// </summary>
    public System.Reflection.PortableExecutable.Subsystem Subsystem;

    /// <summary>
    /// Flags indicating the DLL characteristics of the image.
    /// </summary>
    public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics;
}