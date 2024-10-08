// Copyright (c) Alexandre Mutel. All rights reserved.
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

        if (!reader.File.TryFindSectionByRVA(exportDirectory.Name, out _))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"Unable to find the section for Name {exportDirectory.Name}");
            return;
        }

        // Link to a fake section data until we have recorded the different export tables in the sections
        // Store a fake RVO that is the RVA until we resolve it in the Bind phase
        NameLink = new PEAsciiStringLink(PEStreamSectionData.Empty, (RVO)(uint)exportDirectory.Name);

        // Not sure this one happen
        if (exportDirectory.AddressOfFunctions != 0)
        {
            if (!reader.File.TryFindSectionByRVA(exportDirectory.AddressOfFunctions, out var sectionAddressOfFunctions))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfFunctions, $"Unable to find the section for AddressOfFunctions {exportDirectory.AddressOfFunctions}");
                return;
            }
            ExportFunctionAddressTable = new PEExportAddressTable((int)exportDirectory.NumberOfFunctions)
            {
                Position = sectionAddressOfFunctions.Position + exportDirectory.AddressOfFunctions - sectionAddressOfFunctions.RVA,
                Size = (ulong)(exportDirectory.NumberOfFunctions * sizeof(RVA))
            };
        }

        // AddressOfNames can be 0
        if (exportDirectory.AddressOfNames != 0)
        {
            if (!reader.File.TryFindSectionByRVA(exportDirectory.AddressOfNames, out var sectionAddressOfNames))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfNames, $"Unable to find the section for AddressOfNames {exportDirectory.AddressOfNames}");
                return;
            }

            ExportNameTable = new PEExportNameTable((int)exportDirectory.NumberOfNames)
            {
                Position = sectionAddressOfNames.Position + exportDirectory.AddressOfNames - sectionAddressOfNames.RVA,
                Size = (ulong)(exportDirectory.NumberOfNames * sizeof(RVA))
            };
        }

        // AddressOfNames can be 0
        if (exportDirectory.AddressOfNameOrdinals != 0)
        {
            if (!reader.File.TryFindSectionByRVA(exportDirectory.AddressOfNameOrdinals, out var sectionAddressOfNameOrdinals))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidAddressOfNameOrdinals, $"Unable to find the section for AddressOfNameOrdinals {exportDirectory.AddressOfNameOrdinals}");
                return;
            }

            ExportOrdinalTable = new PEExportOrdinalTable((int)exportDirectory.NumberOfNames)
            {
                Position = sectionAddressOfNameOrdinals.Position + exportDirectory.AddressOfNameOrdinals - sectionAddressOfNameOrdinals.RVA,
                Size = (ulong)(exportDirectory.NumberOfNames * sizeof(ushort))
            };
        }
        
        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        var diagnostics = reader.Diagnostics;

        if (!peFile.TryFindByRVA((RVA)(uint)NameLink.RVO, out var container))
        {
            diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"Unable to find the section data for Name {(RVA)(uint)NameLink.RVO}");
            return;
        }

        var streamSectionData = container as PEStreamSectionData;
        if (streamSectionData is null)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_ExportDirectoryInvalidName, $"The section data for Name {(RVA)(uint)NameLink.RVO} is not a stream section data");
            return;
        }

        NameLink = new PEAsciiStringLink(streamSectionData, NameLink.RVO - streamSectionData.RVA);

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
        var exportDirectory = new RawImageExportDirectory
        {
            TimeDateStamp = (uint)(TimeStamp - DateTime.UnixEpoch).TotalSeconds,
            MajorVersion = MajorVersion,
            MinorVersion = MinorVersion,
            Base = OrdinalBase,
            Name = NameLink.RVA(),
            NumberOfFunctions = (uint)(ExportFunctionAddressTable?.Values.Count ?? 0),
            NumberOfNames = (uint)(ExportNameTable?.Values.Count ?? 0),
            AddressOfFunctions = (RVA)(uint)(ExportFunctionAddressTable?.RVA ?? (RVA)0),
            AddressOfNames = (RVA)(uint)(ExportNameTable?.RVA ?? 0),
            AddressOfNameOrdinals = (RVA)(uint)(ExportOrdinalTable?.RVA ?? 0)
        };

        writer.Write(exportDirectory);
    }

    protected override unsafe uint ComputeHeaderSize(PELayoutContext context)
    {
        return (uint)sizeof(RawImageExportDirectory);
    }

    public override void Verify(PEVerifyContext context)
    {
        context.VerifyObject(NameLink.Container, this, $"the {nameof(NameLink)} of the {nameof(PEExportDirectory)}", false);
        context.VerifyObject(ExportFunctionAddressTable, this, $"the {nameof(ExportFunctionAddressTable)} of the {nameof(PEExportDirectory)}", true);
        context.VerifyObject(ExportNameTable, this, $"the {nameof(ExportNameTable)} of the {nameof(PEExportDirectory)}", true);
        context.VerifyObject(ExportOrdinalTable, this, $"the {nameof(ExportOrdinalTable)} of the {nameof(PEExportDirectory)}", true);

        base.Verify(context);
    }

    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
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