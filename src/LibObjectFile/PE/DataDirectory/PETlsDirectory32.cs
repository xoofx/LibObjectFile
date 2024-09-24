// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the 32-bit Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public sealed class PETlsDirectory32 : PETlsDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    public unsafe PETlsDirectory32() : base(sizeof(PETlsDirectoryData32))
    {
    }
    
    public override unsafe int RawDataSize => sizeof(PETlsDirectoryData32);

    /// <summary>
    /// The starting address of the TLS template.
    /// </summary>
    /// <remarks>
    /// The template is a block of data that is used to initialize TLS data.
    /// The system copies all of this data each time a thread is created, so it must not be corrupted.
    /// Note that this address is not an RVA; it is an address for which there should be a base relocation in the .reloc section.
    /// </remarks>
    public VA32 StartAddressOfRawData
    {
        get => Data.StartAddressOfRawData;
        set => Data.StartAddressOfRawData = value;
    }

    /// <summary>
    /// Gets or sets the address of the last byte of the TLS, except for the zero fill.
    /// </summary>
    /// <remarks>
    /// As with the Raw Data Start VA field, this is a VA, not an RVA.
    /// </remarks>
    public VA32 EndAddressOfRawData
    {
        get => Data.EndAddressOfRawData;
        set => Data.EndAddressOfRawData = value;
    }

    /// <summary>
    /// Gets or sets the location to receive the TLS index, which the loader assigns.
    /// </summary>
    /// <remarks>
    /// This location is in an ordinary data section, so it can be given a symbolic name that is accessible to the program.
    /// </remarks>
    public VA32 AddressOfIndex
    {
        get => Data.AddressOfIndex;
        set => Data.AddressOfIndex = value;
    }

    /// <summary>
    /// Gets or sets the address of the TLS callback functions array.
    /// </summary>
    /// <remarks>
    /// The array is null-terminated, so the number of functions is the difference between this field and the AddressOfCallBacks field, divided by the size of a pointer.
    /// </remarks>
    public VA32 AddressOfCallBacks
    {
        get => Data.AddressOfCallBacks;
        set => Data.AddressOfCallBacks = value;
    }

    /// <summary>
    /// Gets or sets the size in bytes of the template, beyond the initialized data delimited by the Raw Data Start VA and Raw Data End VA fields.
    /// </summary>
    /// <remarks>
    /// The total template size should be the same as the total size of TLS data in the image file. The zero fill is the amount of data that comes after the initialized nonzero data.
    /// </remarks>
    public uint SizeOfZeroFill
    {
        get => Data.SizeOfZeroFill;
        set => Data.SizeOfZeroFill = value;
    }

    /// <summary>
    /// Gets or sets the alignment characteristics of the TLS directory.
    /// </summary>
    public PETlsCharacteristics Characteristics
    {
        get => Data.Characteristics;
        set => Data.Characteristics = value;
    }
    
    /// <summary>
    /// Gets the 32-bit Thread Local Storage (TLS) directory.
    /// </summary>
    public ref PETlsDirectoryData32 Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.As<byte, PETlsDirectoryData32>(ref MemoryMarshal.GetArrayDataReference(RawData));
    }

    public override unsafe void SetRawDataSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, sizeof(PETlsDirectoryData32));
        base.SetRawDataSize(value);
    }
}