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
                var entry = entries[i];
                var debugEntry = new PEDebugDirectoryEntry
                {
                    Characteristics = entry.Characteristics,
                    MajorVersion = entry.MajorVersion,
                    MinorVersion = entry.MinorVersion,
                    Type = entry.Type,
                };


                if (entry.AddressOfRawData != 0)
                {
                    if (!reader.File.TryFindSection(entry.AddressOfRawData, out var section))
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_DebugDirectorySectionNotFound, $"Unable to find the section for the debug directory entry at {entry.AddressOfRawData}");
                        continue;
                    }

                    var dataLink = new PEBlobDataLink(section, (RVO)(uint)entry.AddressOfRawData, entry.SizeOfData);
                    debugEntry.DataLink = dataLink;
                }
                else if (entry.PointerToRawData != 0)
                {
                    var dataLink = new PEBlobDataLink(reader.File, entry.PointerToRawData, entry.SizeOfData);
                    debugEntry.DataLink = dataLink;
                }
                else
                {
                    Debug.Assert(entry.SizeOfData == 0);
                }

                Entries.Add(debugEntry);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override void Bind(PEImageReader reader)
    {
        var entries = CollectionsMarshal.AsSpan(Entries);
        var peFile = reader.File;
        foreach (var entry in entries)
        {

            if (entry.DataLink.Container is PEFile)
            {
                PEObjectBase? container = null;

                foreach (var extraData in peFile.ExtraDataBeforeSections)
                {
                    if (peFile.Contains(entry.DataLink.RVO))
                    {
                        container = extraData;
                        break;
                    }
                }

                if (container is null)
                {
                    foreach (var section in peFile.ExtraDataAfterSections)
                    {
                        if (section.Contains(entry.DataLink.RVO))
                        {
                            container = section;
                            break;
                        }
                    }
                }

                if (container is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_DebugDirectoryContainerNotFound, $"Unable to find the container for the debug directory entry at {entry.DataLink.RVO}");
                    continue;
                }

                entry.DataLink = new(container, entry.DataLink.RVO - (uint)container.Position, entry.DataLink.Size);
            }
            else if (entry.DataLink.Container is PESection)
            {
                var section = (PESection)entry.DataLink.Container!;

                if (!section.TryFindSectionData((RVA)(uint)entry.DataLink.RVO, out var container))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_DebugDirectoryContainerNotFound, $"Unable to find the container for the debug directory entry at {entry.DataLink.RVO}");
                    continue;
                }

                entry.DataLink = new(container, entry.DataLink.RVO - container.RVA, entry.DataLink.Size);
            }
            else
            {
                // Ignore, there are no links
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