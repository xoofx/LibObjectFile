// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Drawing;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Thread Local Storage (TLS) directory in a PE file.
/// </summary>
public sealed class PETlsDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PETlsDirectory"/> class.
    /// </summary>
    public PETlsDirectory() : base(PEDataDirectoryKind.Tls)
    {
        StartAddressOfRawDataLink = new VALink(this);
        EndAddressOfRawDataLink = new VALink(this);
        AddressOfIndexLink = new VALink(this);
        AddressOfCallBacksLink = new VALink(this);
    }

    /// <summary>
    /// The starting address of the TLS template.
    /// </summary>
    /// <remarks>
    /// The template is a block of data that is used to initialize TLS data.
    /// The system copies all of this data each time a thread is created, so it must not be corrupted.
    /// Note that this address is not an RVA; it is an address for which there should be a base relocation in the .reloc section.
    /// </remarks>
    public VALink StartAddressOfRawDataLink { get; }

    /// <summary>
    /// Gets or sets the address of the last byte of the TLS, except for the zero fill.
    /// </summary>
    /// <remarks>
    /// As with the Raw Data Start VA field, this is a VA, not an RVA.
    /// </remarks>
    public VALink EndAddressOfRawDataLink { get; }

    /// <summary>
    /// Gets or sets the location to receive the TLS index, which the loader assigns.
    /// </summary>
    /// <remarks>
    /// This location is in an ordinary data section, so it can be given a symbolic name that is accessible to the program.
    /// </remarks>
    public VALink AddressOfIndexLink { get; }

    /// <summary>
    /// Gets or sets the address of the TLS callback functions array.
    /// </summary>
    /// <remarks>
    /// The array is null-terminated, so the number of functions is the difference between this field and the AddressOfCallBacks field, divided by the size of a pointer.
    /// </remarks>
    public VALink AddressOfCallBacksLink { get; }

    /// <summary>
    /// Gets or sets the size in bytes of the template, beyond the initialized data delimited by the Raw Data Start VA and Raw Data End VA fields.
    /// </summary>
    /// <remarks>
    /// The total template size should be the same as the total size of TLS data in the image file. The zero fill is the amount of data that comes after the initialized nonzero data.
    /// </remarks>
    public uint SizeOfZeroFill { get; set; }

    /// <summary>
    /// Gets or sets the alignment characteristics of the TLS directory.
    /// </summary>
    public PETlsCharacteristics Characteristics { get; set; }

    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        return context.File.IsPE32 ? (uint)sizeof(RawTlsDirectory32) : (uint)sizeof(RawTlsDirectory64);
    }
    
    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        if (reader.File.IsPE32)
        {
            Read32(reader);
        }
        else
        {
            Read64(reader);
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    private unsafe void Read32(PEImageReader reader)
    {
        RawTlsDirectory32 entry;
        if (!reader.TryReadData(sizeof(RawTlsDirectory32), out entry))
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PETlsDirectory)}");
            return;
        }

        StartAddressOfRawDataLink.SetTempAddress(reader, entry.StartAddressOfRawData);
        EndAddressOfRawDataLink.SetTempAddress(reader, entry.EndAddressOfRawData);
        AddressOfIndexLink.SetTempAddress(reader, entry.AddressOfIndex);
        AddressOfCallBacksLink.SetTempAddress(reader, entry.AddressOfCallBacks);


        SizeOfZeroFill = entry.SizeOfZeroFill;
        Characteristics = entry.Characteristics;
    }

    private unsafe void Read64(PEImageReader reader)
    {
        RawTlsDirectory64 entry;
        if (!reader.TryReadData(sizeof(RawTlsDirectory64), out entry))
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while reading {nameof(PETlsDirectory)}");
            return;
        }
        
        StartAddressOfRawDataLink.SetTempAddress(reader, entry.StartAddressOfRawData);
        EndAddressOfRawDataLink.SetTempAddress(reader, entry.EndAddressOfRawData);
        AddressOfIndexLink.SetTempAddress(reader, entry.AddressOfIndex);
        AddressOfCallBacksLink.SetTempAddress(reader, entry.AddressOfCallBacks);
        
        SizeOfZeroFill = entry.SizeOfZeroFill;
        Characteristics = entry.Characteristics;
    }

    internal override void Bind(PEImageReader reader)
    {
        if (!StartAddressOfRawDataLink.TryBind(reader))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidTlsStartAddressOfRawData, $"Invalid TLS StartAddressOfRawData {StartAddressOfRawDataLink.Offset}");
        }

        if (!EndAddressOfRawDataLink.TryBind(reader, true))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidTlsEndAddressOfRawData, $"Invalid TLS EndAddressOfRawData {EndAddressOfRawDataLink.Offset}");
        }

        if (!AddressOfIndexLink.TryBind(reader))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidTlsAddressOfIndex, $"Invalid TLS AddressOfIndex {AddressOfIndexLink.Offset}");
        }

        if (!AddressOfCallBacksLink.TryBind(reader))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidTlsAddressOfCallBacks, $"Invalid TLS AddressOfCallBacks {AddressOfCallBacksLink.Offset}");
        }
    }

    public override void Write(PEImageWriter writer)
    {
    }

    public override int ReadAt(uint offset, Span<byte> destination)
    {
        var peFile = GetPEFile();
        if (peFile is null)
        {
            destination.Fill(0);
            return 0;
        }

        bool isPE32 = peFile.IsPE32;

        int size = 0;
        if (isPE32)
        {
            if (destination.Length != 4)
            {
                return 0;
            }

            switch (offset)
            {
                case 0:
                    if (StartAddressOfRawDataLink.TryGetVA(out var startAddressOfRawData))
                    {
                        startAddressOfRawData.Write32(destination);
                        size = 4;
                    }

                    break;
                case 4:
                    if (EndAddressOfRawDataLink.TryGetVA(out var endAddressOfRawData))
                    {
                        endAddressOfRawData.Write32(destination);
                        size = 4;
                    }

                    break;
                case 8:
                    if (AddressOfIndexLink.TryGetVA(out var addressOfIndex))
                    {
                        addressOfIndex.Write32(destination);
                        size = 4;
                    }

                    break;
                case 12:
                    if (AddressOfCallBacksLink.TryGetVA(out var addressOfCallBacks))
                    {
                        addressOfCallBacks.Write32(destination);
                        size = 4;
                    }

                    break;
            }
        }
        else
        {
            if (destination.Length != 8)
            {
                return 0;
            }

            switch (offset)
            {
                case 0:
                    if (StartAddressOfRawDataLink.TryGetVA(out var startAddressOfRawData))
                    {
                        startAddressOfRawData.Write64(destination);
                        size = 8;
                    }

                    break;
                case 8:
                    if (EndAddressOfRawDataLink.TryGetVA(out var endAddressOfRawData))
                    {
                        endAddressOfRawData.Write64(destination);
                        size = 8;
                    }

                    break;
                case 16:
                    if (AddressOfIndexLink.TryGetVA(out var addressOfIndex))
                    {
                        addressOfIndex.Write64(destination);
                        size = 8;
                    }

                    break;
                case 24:
                    if (AddressOfCallBacksLink.TryGetVA(out var addressOfCallBacks))
                    {
                        addressOfCallBacks.Write64(destination);
                        size = 8;
                    }

                    break;
            }
        }

        return size;
    }

    public override void WriteAt(uint offset, ReadOnlySpan<byte> source)
    {
        
    }


#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct RawTlsDirectory32
    {
        public uint StartAddressOfRawData;
        public uint EndAddressOfRawData;
        public uint AddressOfIndex;
        public uint AddressOfCallBacks;
        public uint SizeOfZeroFill;
        public PETlsCharacteristics Characteristics;
    }

    private struct RawTlsDirectory64
    {
        public ulong StartAddressOfRawData;
        public ulong EndAddressOfRawData;
        public ulong AddressOfIndex;
        public ulong AddressOfCallBacks;
        public uint SizeOfZeroFill;
        public PETlsCharacteristics Characteristics;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}