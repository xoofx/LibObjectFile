﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEExportDirectory : PEDataDirectory
{
    public PEExportDirectory() : base(PEDataDirectoryKind.Export)
    {
        OrdinalBase = 1;
    }

    public DateTime TimeStamp { get; set; }

    public ushort MajorVersion { get; set; }

    public ushort MinorVersion { get; set; }

    public uint OrdinalBase { get; set; }

    public PEAsciiStringLink NameLink { get; set; }

    public PEExportAddressTable? ExportFunctionAddressTable { get; set; }

    public PEExportNameTable? ExportNameTable { get; set; }

    public PEExportOrdinalTable? ExportOrdinalTable { get; set; }

    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        return (uint)sizeof(RawImageExportDirectory);
    }

    internal override IEnumerable<PESectionData> CollectImplicitSectionDataList()
    {
        if (ExportFunctionAddressTable is not null)
        {
            yield return ExportFunctionAddressTable;
        }

        if (ExportNameTable is not null)
        {
            yield return ExportNameTable;
        }

        if (ExportOrdinalTable is not null)
        {
            yield return ExportOrdinalTable;
        }
    }

    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        if (!reader.TryReadData(sizeof(RawImageExportDirectory), out RawImageExportDirectory exportDirectory))
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Directory");
            return;
        }

        TimeStamp = DateTime.UnixEpoch.AddSeconds(exportDirectory.TimeDateStamp);
        MajorVersion = exportDirectory.MajorVersion;
        MinorVersion = exportDirectory.MinorVersion;
        OrdinalBase = (ushort)exportDirectory.Base;

        if (!reader.File.TryFindSection(exportDirectory.Name, out _))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"Unable to find the section for Name {exportDirectory.Name}");
            return;
        }

        // Link to a fake section data until we have recorded the different export tables in the sections
        NameLink = new PEAsciiStringLink(PEStreamSectionData.Empty, exportDirectory.Name);
        
        if (!reader.File.TryFindSection(exportDirectory.AddressOfFunctions, out var sectionAddressOfFunctions))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfFunctions, $"Unable to find the section for AddressOfFunctions {exportDirectory.AddressOfFunctions}");
            return;
        }

        if (!reader.File.TryFindSection(exportDirectory.AddressOfNames, out var sectionAddressOfNames))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfNames, $"Unable to find the section for AddressOfNames {exportDirectory.AddressOfNames}");
            return;
        }

        if (!reader.File.TryFindSection(exportDirectory.AddressOfNameOrdinals, out var sectionAddressOfNameOrdinals))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfNameOrdinals, $"Unable to find the section for AddressOfNameOrdinals {exportDirectory.AddressOfNameOrdinals}");
            return;
        }

        ExportFunctionAddressTable = new PEExportAddressTable((int)exportDirectory.NumberOfFunctions)
        {
            Position = sectionAddressOfFunctions.Position + exportDirectory.AddressOfFunctions - sectionAddressOfFunctions.VirtualAddress,
            Size = (ulong)(exportDirectory.NumberOfFunctions * sizeof(RVA))
        };

        ExportNameTable = new PEExportNameTable((int)exportDirectory.NumberOfNames)
        {
            Position = sectionAddressOfNames.Position + exportDirectory.AddressOfNames - sectionAddressOfNames.VirtualAddress,
            Size = (ulong)(exportDirectory.NumberOfNames * sizeof(RVA))
        };

        ExportOrdinalTable = new PEExportOrdinalTable((int)exportDirectory.NumberOfFunctions)
        {
            Position = sectionAddressOfNameOrdinals.Position + exportDirectory.AddressOfNameOrdinals - sectionAddressOfNameOrdinals.VirtualAddress,
            Size = (ulong)(exportDirectory.NumberOfFunctions * sizeof(ushort))
        };

        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        if (!peFile.TryFindVirtualContainer(NameLink.RVA(), out var container))
        {
            diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"Unable to find the section data for Name {NameLink.RVA()}");
            return;
        }

        var streamSectionData = container as PEStreamSectionData;
        if (streamSectionData is null)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"The section data for Name {NameLink.RVA()} is not a stream section data");
            return;
        }

        NameLink = new PEAsciiStringLink(streamSectionData, NameLink.Offset);

        if (ExportFunctionAddressTable is not null)
        {
            ExportFunctionAddressTable.Read(reader);
        }

        if (ExportNameTable is not null)
        {
            ExportNameTable.Read(reader);
        }

        if (ExportOrdinalTable is not null)
        {
            ExportOrdinalTable.Read(reader);
        }
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    private struct RawImageExportDirectory
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public RVA Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public RVA AddressOfFunctions;     // RVA from base of image
        public RVA AddressOfNames;         // RVA from base of image
        public RVA AddressOfNameOrdinals;  // RVA from base of image
    }
}