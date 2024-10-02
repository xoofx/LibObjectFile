// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.PortableExecutable;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEOptionalHeader
{
    internal RawImageOptionalHeaderCommonPart1 OptionalHeaderCommonPart1;
    internal RawImageOptionalHeaderBase32 OptionalHeaderBase32;
    internal RawImageOptionalHeaderBase64 OptionalHeaderBase64;
    internal RawImageOptionalHeaderCommonPart2 OptionalHeaderCommonPart2;
    internal RawImageOptionalHeaderSize32 OptionalHeaderSize32;
    internal RawImageOptionalHeaderSize64 OptionalHeaderSize64;
    internal RawImageOptionalHeaderCommonPart3 OptionalHeaderCommonPart3;

    private readonly PEFile _peFile;
    private PESection? _baseOfCode;
    private PESectionDataLink _entryPointLink;

    internal PEOptionalHeader(PEFile peFile)
    {
        _peFile = peFile;
    }

    internal PEOptionalHeader(PEFile peFile, PEOptionalHeaderMagic magic)
    {
        _peFile = peFile;

        // Clear all fields
        OptionalHeaderCommonPart1 = default;
        OptionalHeaderBase32 = default;
        OptionalHeaderBase64 = default;
        OptionalHeaderCommonPart2 = default;
        OptionalHeaderSize32 = default;
        OptionalHeaderSize64 = default;
        OptionalHeaderCommonPart3 = default;

        // Setup some fields to some default values
        OptionalHeaderCommonPart1.Magic = magic;
        OptionalHeaderCommonPart1.MajorLinkerVersion = 1;

        OptionalHeaderBase64.ImageBase = magic == PEOptionalHeaderMagic.PE32 ? 0x1400_0000U :  0x14000_0000UL;

        OptionalHeaderCommonPart2.SectionAlignment = 0x1000;
        OptionalHeaderCommonPart2.FileAlignment = 0x200;
        OptionalHeaderCommonPart2.MajorOperatingSystemVersion = 6;
        OptionalHeaderCommonPart2.MajorSubsystemVersion = 6;

        OptionalHeaderCommonPart2.Subsystem = Subsystem.WindowsCui;
        OptionalHeaderCommonPart2.DllCharacteristics = DllCharacteristics.HighEntropyVirtualAddressSpace
                                                       | DllCharacteristics.DynamicBase
                                                       | DllCharacteristics.TerminalServerAware;

        OptionalHeaderSize64.SizeOfStackReserve = 0x100_000;
        OptionalHeaderSize64.SizeOfStackCommit = 0x1000;
        OptionalHeaderSize64.SizeOfHeapReserve = 0x100_000;
        OptionalHeaderSize64.SizeOfHeapCommit = 0x1000;

        OptionalHeaderCommonPart3.NumberOfRvaAndSizes = 16;
    }

    /// <summary>
    /// The magic number, which identifies the file format. Expected to be 0x10b for PE32.
    /// </summary>
    /// <remarks>
    /// This value cannot be changed and must be set at construction time.
    /// </remarks>
    public PEOptionalHeaderMagic Magic
    {
        get => OptionalHeaderCommonPart1.Magic;
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
    }

    /// <summary>
    /// The size of the initialized data section, in bytes.
    /// </summary>
    public uint SizeOfInitializedData
    {
        get => OptionalHeaderCommonPart1.SizeOfInitializedData;
    }

    /// <summary>
    /// The size of the uninitialized data section, in bytes.
    /// </summary>
    public uint SizeOfUninitializedData
    {
        get => OptionalHeaderCommonPart1.SizeOfUninitializedData;
    }

    /// <summary>
    /// The address of the entry point relative to the image base when the executable starts.
    /// </summary>
    public PESectionDataLink AddressOfEntryPoint
    {
        get => _entryPointLink;
        set => _entryPointLink = value;
    }

    /// <summary>
    /// The address relative to the image base of the beginning of the code section.
    /// </summary>
    public PESection? BaseOfCode
    {
        get
        {
            return _baseOfCode;
        }
        set
        {
            _baseOfCode = value;
            OptionalHeaderCommonPart1.BaseOfCode = _baseOfCode?.RVA ?? 0;
        }
    }

    /// <summary>
    /// The address relative to the image base of the beginning of the data section.
    /// </summary>
    /// <remarks>
    /// Only valid for PE32.
    /// </remarks>
    public RVA BaseOfData
    {
        get => OptionalHeaderBase32.BaseOfData;
        set => OptionalHeaderBase32.BaseOfData = value;
    }
    
    // NT additional fields.

    /// <summary>
    /// The preferred address of the first byte of the image when loaded into memory.
    /// </summary>
    /// <remarks>
    /// In order to change the ImageBase use <see cref="PEFile.Relocate"/>
    /// </remarks>
    public ulong ImageBase
    {
        get => OptionalHeaderBase64.ImageBase;
        internal set => OptionalHeaderBase64.ImageBase = value;
    }

    /// <summary>
    /// The alignment of sections in memory, in bytes.
    /// </summary>
    public uint SectionAlignment
    {
        get => OptionalHeaderCommonPart2.SectionAlignment;
        set
        {
            if (value == 0 || !BitOperations.IsPow2(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "SectionAlignment must be a power of 2 and not zero");
            }

            if (SectionAlignment < FileAlignment)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "SectionAlignment must be greater than or equal to FileAlignment");
            }
            
            OptionalHeaderCommonPart2.SectionAlignment = value;
        }
    }

    /// <summary>
    /// The alignment of the raw data of sections in the image file, in bytes.
    /// </summary>
    public uint FileAlignment
    {
        get => OptionalHeaderCommonPart2.FileAlignment;
        set
        {
            if (value == 0 || !BitOperations.IsPow2(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "FileAlignment must be a power of 2 and not zero");
            }
            
            OptionalHeaderCommonPart2.FileAlignment = value;
        }
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
    }

    /// <summary>
    /// The combined size of all headers (DOS, PE, section headers), rounded up to a multiple of FileAlignment.
    /// </summary>
    public uint SizeOfHeaders
    {
        get => OptionalHeaderCommonPart2.SizeOfHeaders;
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
        get => (uint)_peFile.Directories.Count;
    }
    
    internal void SyncPE32PlusToPE32()
    {
        OptionalHeaderBase32.ImageBase = (uint)OptionalHeaderBase64.ImageBase;
        OptionalHeaderSize32.SizeOfStackReserve = (uint)OptionalHeaderSize64.SizeOfStackReserve;
        OptionalHeaderSize32.SizeOfStackCommit = (uint)OptionalHeaderSize64.SizeOfStackCommit;
        OptionalHeaderSize32.SizeOfHeapReserve = (uint)OptionalHeaderSize64.SizeOfHeapReserve;
        OptionalHeaderSize32.SizeOfHeapCommit = (uint)OptionalHeaderSize64.SizeOfHeapCommit;
    }

    internal void SyncPE32ToPE32Plus()
    {
        OptionalHeaderBase64.ImageBase = OptionalHeaderBase32.ImageBase;
        OptionalHeaderSize64.SizeOfStackReserve = OptionalHeaderSize32.SizeOfStackReserve;
        OptionalHeaderSize64.SizeOfStackCommit = OptionalHeaderSize32.SizeOfStackCommit;
        OptionalHeaderSize64.SizeOfHeapReserve = OptionalHeaderSize32.SizeOfHeapReserve;
        OptionalHeaderSize64.SizeOfHeapCommit = OptionalHeaderSize32.SizeOfHeapCommit;
    }
}