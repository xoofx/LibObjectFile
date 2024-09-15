// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public struct ImageOptionalHeader
{
    internal RawImageOptionalHeaderCommonPart1 OptionalHeaderCommonPart1;
    internal RawImageOptionalHeaderBase32 OptionalHeaderBase32;
    internal RawImageOptionalHeaderBase64 OptionalHeaderBase64;
    internal RawImageOptionalHeaderCommonPart2 OptionalHeaderCommonPart2;
    internal RawImageOptionalHeaderSize32 OptionalHeaderSize32;
    internal RawImageOptionalHeaderSize64 OptionalHeaderSize64;
    internal RawImageOptionalHeaderCommonPart3 OptionalHeaderCommonPart3;

    /// <summary>
    /// The magic number, which identifies the file format. Expected to be 0x10b for PE32.
    /// </summary>
    public ImageOptionalHeaderMagic Magic
    {
        get => OptionalHeaderCommonPart1.Magic;
        set => OptionalHeaderCommonPart1.Magic = value;
    }

    /// <summary>
    /// The major version number of the linker.
    /// </summary>
    public byte MajorLinkerVersion
    {
        get => OptionalHeaderCommonPart1.MajorLinkerVersion;
        set => OptionalHeaderCommonPart1.MajorLinkerVersion = value;
    }

    /// <summary>
    /// The minor version number of the linker.
    /// </summary>
    public byte MinorLinkerVersion
    {
        get => OptionalHeaderCommonPart1.MinorLinkerVersion;
        set => OptionalHeaderCommonPart1.MinorLinkerVersion = value;
    }

    /// <summary>
    /// The size of the code (text) section, in bytes.
    /// </summary>
    public uint SizeOfCode
    {
        get => OptionalHeaderCommonPart1.SizeOfCode;
        set => OptionalHeaderCommonPart1.SizeOfCode = value;
    }

    /// <summary>
    /// The size of the initialized data section, in bytes.
    /// </summary>
    public uint SizeOfInitializedData
    {
        get => OptionalHeaderCommonPart1.SizeOfInitializedData;
        set => OptionalHeaderCommonPart1.SizeOfInitializedData = value;
    }

    /// <summary>
    /// The size of the uninitialized data section, in bytes.
    /// </summary>
    public uint SizeOfUninitializedData
    {
        get => OptionalHeaderCommonPart1.SizeOfUninitializedData;
        set => OptionalHeaderCommonPart1.SizeOfUninitializedData = value;
    }

    /// <summary>
    /// The address of the entry point relative to the image base when the executable starts.
    /// </summary>
    public uint AddressOfEntryPoint
    {
        get => OptionalHeaderCommonPart1.AddressOfEntryPoint;
        set => OptionalHeaderCommonPart1.AddressOfEntryPoint = value;
    }

    /// <summary>
    /// The address relative to the image base of the beginning of the code section.
    /// </summary>
    public uint BaseOfCode
    {
        get => OptionalHeaderCommonPart1.BaseOfCode;
        set => OptionalHeaderCommonPart1.BaseOfCode = value;
    }

    /// <summary>
    /// The address relative to the image base of the beginning of the data section.
    /// </summary>
    /// <remarks>
    /// Only valid for PE32.
    /// </remarks>
    public uint BaseOfData
    {
        get => OptionalHeaderBase32.BaseOfData;
        set => OptionalHeaderBase32.BaseOfData = value;
    }
    
    // NT additional fields.

    /// <summary>
    /// The preferred address of the first byte of the image when loaded into memory.
    /// </summary>
    public ulong ImageBase
    {
        get => OptionalHeaderBase64.ImageBase;
        set => OptionalHeaderBase64.ImageBase = value;
    }

    /// <summary>
    /// The alignment of sections in memory, in bytes.
    /// </summary>
    public uint SectionAlignment
    {
        get => OptionalHeaderCommonPart2.SectionAlignment;
        set => OptionalHeaderCommonPart2.SectionAlignment = value;
    }

    /// <summary>
    /// The alignment of the raw data of sections in the image file, in bytes.
    /// </summary>
    public uint FileAlignment
    {
        get => OptionalHeaderCommonPart2.FileAlignment;
        set => OptionalHeaderCommonPart2.FileAlignment = value;
    }

    /// <summary>
    /// The major version number of the required operating system.
    /// </summary>
    public ushort MajorOperatingSystemVersion
    {
        get => OptionalHeaderCommonPart2.MajorOperatingSystemVersion;
        set => OptionalHeaderCommonPart2.MajorOperatingSystemVersion = value;
    }

    /// <summary>
    /// The minor version number of the required operating system.
    /// </summary>
    public ushort MinorOperatingSystemVersion
    {
        get => OptionalHeaderCommonPart2.MinorOperatingSystemVersion;
        set => OptionalHeaderCommonPart2.MinorOperatingSystemVersion = value;
    }

    /// <summary>
    /// The major version number of the image.
    /// </summary>
    public ushort MajorImageVersion
    {
        get => OptionalHeaderCommonPart2.MajorImageVersion;
        set => OptionalHeaderCommonPart2.MajorImageVersion = value;
    }

    /// <summary>
    /// The minor version number of the image.
    /// </summary>
    public ushort MinorImageVersion
    {
        get => OptionalHeaderCommonPart2.MinorImageVersion;
        set => OptionalHeaderCommonPart2.MinorImageVersion = value;
    }

    /// <summary>
    /// The major version number of the subsystem.
    /// </summary>
    public ushort MajorSubsystemVersion
    {
        get => OptionalHeaderCommonPart2.MajorSubsystemVersion;
        set => OptionalHeaderCommonPart2.MajorSubsystemVersion = value;
    }

    /// <summary>
    /// The minor version number of the subsystem.
    /// </summary>
    public ushort MinorSubsystemVersion
    {
        get => OptionalHeaderCommonPart2.MinorSubsystemVersion;
        set => OptionalHeaderCommonPart2.MinorSubsystemVersion = value;
    }

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Win32VersionValue
    {
        get => OptionalHeaderCommonPart2.Win32VersionValue;
        set => OptionalHeaderCommonPart2.Win32VersionValue = value;
    }

    /// <summary>
    /// The size of the image, including all headers, as loaded in memory, in bytes. Must be a multiple of SectionAlignment.
    /// </summary>
    public uint SizeOfImage
    {
        get => OptionalHeaderCommonPart2.SizeOfImage;
        set => OptionalHeaderCommonPart2.SizeOfImage = value;
    }

    /// <summary>
    /// The combined size of all headers (DOS, PE, section headers), rounded up to a multiple of FileAlignment.
    /// </summary>
    public uint SizeOfHeaders
    {
        get => OptionalHeaderCommonPart2.SizeOfHeaders;
        set => OptionalHeaderCommonPart2.SizeOfHeaders = value;
    }

    /// <summary>
    /// The image file checksum.
    /// </summary>
    public uint CheckSum
    {
        get => OptionalHeaderCommonPart2.CheckSum;
        set => OptionalHeaderCommonPart2.CheckSum = value;
    }

    /// <summary>
    /// The subsystem required to run this image.
    /// </summary>
    public System.Reflection.PortableExecutable.Subsystem Subsystem
    {
        get => OptionalHeaderCommonPart2.Subsystem;
        set => OptionalHeaderCommonPart2.Subsystem = value;
    }

    /// <summary>
    /// Flags indicating the DLL characteristics of the image.
    /// </summary>
    public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics
    {
        get => OptionalHeaderCommonPart2.DllCharacteristics;
        set => OptionalHeaderCommonPart2.DllCharacteristics = value;
    }

    /// <summary>
    /// The size of the stack to reserve, in bytes.
    /// </summary>
    public ulong SizeOfStackReserve
    {
        get => OptionalHeaderSize64.SizeOfStackReserve;
        set => OptionalHeaderSize64.SizeOfStackReserve = value;
    }

    /// <summary>
    /// The size of the stack to commit, in bytes.
    /// </summary>
    public ulong SizeOfStackCommit
    {
        get => OptionalHeaderSize64.SizeOfStackCommit;
        set => OptionalHeaderSize64.SizeOfStackCommit = value;
    }

    /// <summary>
    /// The size of the local heap space to reserve, in bytes.
    /// </summary>
    public ulong SizeOfHeapReserve
    {
        get => OptionalHeaderSize64.SizeOfHeapReserve;
        set => OptionalHeaderSize64.SizeOfHeapReserve = value;
    }

    /// <summary>
    /// The size of the local heap space to commit, in bytes.
    /// </summary>
    public ulong SizeOfHeapCommit
    {
        get => OptionalHeaderSize64.SizeOfHeapCommit;
        set => OptionalHeaderSize64.SizeOfHeapCommit = value;
    }

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint LoaderFlags
    {
        get => OptionalHeaderCommonPart3.LoaderFlags;
        set => OptionalHeaderCommonPart3.LoaderFlags = value;
    }

    /// <summary>
    /// The number of data-directory entries in the remainder of the optional header.
    /// </summary>
    public uint NumberOfRvaAndSizes
    {
        get => OptionalHeaderCommonPart3.NumberOfRvaAndSizes;
        set => OptionalHeaderCommonPart3.NumberOfRvaAndSizes = value;
    }

    /// <summary>
    /// The data directories array, which contains the location and size of special tables in the file.
    /// </summary>
    [UnscopedRef]
    public ref ImageDataDirectoryArray DataDirectory => ref OptionalHeaderCommonPart3.DataDirectory;
    
    internal void SyncPE32PlusToPE32()
    {
        OptionalHeaderBase32.ImageBase = (uint)OptionalHeaderBase64.ImageBase;
        OptionalHeaderSize32.SizeOfStackReserve = (uint)OptionalHeaderSize64.SizeOfStackReserve;
        OptionalHeaderSize32.SizeOfStackCommit = (uint)OptionalHeaderSize64.SizeOfStackCommit;
        OptionalHeaderSize32.SizeOfHeapReserve = (uint)OptionalHeaderSize64.SizeOfHeapReserve;
        OptionalHeaderSize32.SizeOfHeapCommit = (uint)OptionalHeaderSize64.SizeOfHeapCommit;
    }
}