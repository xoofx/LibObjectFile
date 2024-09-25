// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        var positionBeforeFirstSection = reader.File.Sections.Count > 0 ? reader.File.Sections[0].Position : 0;
        var positionAfterLastSection = reader.File.Sections.Count > 0 ? reader.File.Sections[^1].Position + reader.File.Sections[^1].Size : 0;


        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = buffer.AsSpan(0, size);
            var entries = MemoryMarshal.Cast<byte, RawImageDebugDirectory>(span);

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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
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
        throw new NotImplementedException();
    }

    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        return (uint)(Entries.Count * sizeof(RawImageDebugDirectory));
    }
}