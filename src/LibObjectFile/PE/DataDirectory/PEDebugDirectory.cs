// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEDebugDirectory : PEDataDirectory
{
    public PEDebugDirectory() : base(PEDataDirectoryKind.Debug)
    {
        Entries = new();
    }

    public List<PEDebugDirectoryEntry> Entries { get; }
    
    public override unsafe void Read(PEImageReader reader)
    {
        var size = (int)Size;

        var entryCount = size / sizeof(RawImageDebugDirectory);

        // Scope the pooled span to ensure it is returned to the pool as soon as possible
        {
            using var tempSpan = TempSpan<RawImageDebugDirectory>.Create(entryCount, out var entries);
            Span<byte> span = tempSpan;

            reader.Position = Position;
            int read = reader.Read(span);
            if (read != size)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_DebugDirectorySize, $"Invalid size found when trying to read the Debug directory at {Position}");
                return;
            }

            for (int i = 0; i < entryCount; i++)
            {
                var rawEntry = entries[i];
                var entry = new PEDebugDirectoryEntry
                {
                    Characteristics = rawEntry.Characteristics,
                    MajorVersion = rawEntry.MajorVersion,
                    MinorVersion = rawEntry.MinorVersion,
                    TimeDateStamp = rawEntry.TimeDateStamp,
                    Type = rawEntry.Type,
                };


                if (rawEntry.AddressOfRawData != 0)
                {
                    if (!reader.File.TryFindSection(rawEntry.AddressOfRawData, out var section))
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_DebugDirectorySectionNotFound, $"Unable to find the section for the debug directory entry at {rawEntry.AddressOfRawData}");
                        continue;
                    }

                    PESectionData debugSectionData;

                    if (rawEntry.Type == PEDebugKnownType.CodeView)
                    {
                        debugSectionData = new PEDebugSectionDataRSDS();
                    }
                    else
                    {
                        debugSectionData = new PEDebugStreamSectionData();
                    }

                    debugSectionData.Position = section.Position + (RVA)(uint)rawEntry.AddressOfRawData - section.RVA;
                    debugSectionData.Size = rawEntry.SizeOfData;

                    entry.SectionData = debugSectionData;
                }
                else if (rawEntry.PointerToRawData != 0)
                {

                    var extraData = new PEDebugStreamExtraData
                    {
                        Position = (RVA)(uint)rawEntry.PointerToRawData,
                        Size = rawEntry.SizeOfData
                    };

                    entry.ExtraData = extraData;
                }
                else
                {
                    Debug.Assert(rawEntry.SizeOfData == 0);
                }

                Entries.Add(entry);
            }
        }

        // Read the data associated with the debug directory entries
        foreach (var entry in Entries)
        {
            if (entry.SectionData is not null)
            {
                entry.SectionData.Read(reader);
            }
            else if (entry.ExtraData is not null)
            {
                entry.ExtraData.Read(reader);
            }
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
    {
        foreach (var entry in Entries)
        {
            if (entry.SectionData is not null)
            {
                yield return entry.SectionData;
            }
            else if (entry.ExtraData is not null)
            {
                yield return entry.ExtraData;
            }
        }
    }
    
    public override void Write(PEImageWriter writer)
    {
        var entries = CollectionsMarshal.AsSpan(Entries);
        using var tempSpan = TempSpan<RawImageDebugDirectory>.Create(entries.Length, out var rawEntries);
        
        RawImageDebugDirectory rawEntry = default;
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            rawEntry.Characteristics = entry.Characteristics;
            rawEntry.MajorVersion = entry.MajorVersion;
            rawEntry.MinorVersion = entry.MinorVersion;
            rawEntry.TimeDateStamp = entry.TimeDateStamp;
            rawEntry.Type = entry.Type;

            if (entry.SectionData is not null)
            {
                rawEntry.SizeOfData = (uint)entry.SectionData.Size;
                rawEntry.AddressOfRawData = (uint)entry.SectionData.RVA;
                rawEntry.PointerToRawData = 0;
            }
            else if (entry.ExtraData is not null)
            {
                rawEntry.SizeOfData = (uint)entry.ExtraData.Size;
                rawEntry.AddressOfRawData = 0;
                rawEntry.PointerToRawData = (uint)entry.ExtraData.Position;
            }

            rawEntries[i] = rawEntry;
        }

        writer.Write(tempSpan);
    }

    protected override unsafe uint ComputeHeaderSize(PELayoutContext context)
    {
        return (uint)(Entries.Count * sizeof(RawImageDebugDirectory));
    }
}