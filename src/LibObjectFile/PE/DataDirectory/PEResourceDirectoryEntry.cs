// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a directory entry in the Portable Executable (PE) <see cref="PEResourceDirectory"/>.
/// </summary>
/// <remarks>
/// This class provides functionality to manage a directory entry in the  <see cref="PEResourceDirectory"/>.
/// It allows adding, removing, and updating resource entries within the directory.
/// </remarks>
public sealed class PEResourceDirectoryEntry : PEResourceEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDirectoryEntry"/> class.
    /// </summary>
    public PEResourceDirectoryEntry()
    {
        ByNames = new();
        ByIds = new();
    }

    /// <summary>
    /// Gets or sets the characteristics of the resource directory entry.
    /// </summary>
    public uint Characteristics { get; set; }

    /// <summary>
    /// Gets or sets the time stamp of the resource directory entry.
    /// </summary>
    public DateTime TimeDateStamp { get; set; }

    /// <summary>
    /// Gets or sets the major version of the resource directory entry.
    /// </summary>
    public ushort MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets the minor version of the resource directory entry.
    /// </summary>
    public ushort MinorVersion { get; set; }

    /// <summary>
    /// Gets the list of resource entries within the directory.
    /// </summary>
    public List<PEResourceDirectoryEntryByName> ByNames { get; }

    /// <summary>
    /// Gets the list of resource entries within the directory.
    /// </summary>
    public List<PEResourceDirectoryEntryById> ByIds { get; }
    
    internal override unsafe void Read(in ReaderContext context)
    {
        var reader = context.Reader;
        
        reader.Position = Position;
        if (!reader.TryReadData<RawImageResourceDirectory>(sizeof(RawImageResourceDirectory), out var data))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory at position {reader.Position}");
            return;
        }

        Characteristics = data.Characteristics;
        TimeDateStamp = DateTime.UnixEpoch.AddSeconds(data.TimeDateStamp);
        MajorVersion = data.MajorVersion;
        MinorVersion = data.MinorVersion;

        var buffer = new byte[(data.NumberOfNamedEntries + data.NumberOfIdEntries) * sizeof(RawImageResourceDirectoryEntry)];
        var spanEntries = MemoryMarshal.Cast<byte, RawImageResourceDirectoryEntry>(buffer);

        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory at position {reader.Position}");
            return;
        }

        // Read all entries
        for (int i = 0; i < data.NumberOfNamedEntries + data.NumberOfIdEntries; i++)
        {
            var entry = spanEntries[i];
            ReadEntry(context, entry);

            if (reader.Diagnostics.HasErrors)
            {
                return;
            }
        }

        // Update the size
        Size = reader.Position - Position;

        Debug.Assert(Size == CalculateSize());

        // Read all strings (they should follow the directory)
        var byNames = CollectionsMarshal.AsSpan(ByNames);
        foreach (ref var item in byNames)
        {
            context.Strings.Add(item.Name);
            context.Entries.Add(item.Entry);
            item.Entry.Read(context);
        }

        // Read the entry content 
        var byIds = CollectionsMarshal.AsSpan(ByIds);
        foreach (ref var item in byIds)
        {
            context.Entries.Add(item.Entry);
            item.Entry.Read(context);
        }
    }

    public override unsafe void Write(PEImageWriter writer)
    {
        var size = Size;
        Debug.Assert(size == CalculateSize());
        var directory = (PEResourceDirectory)Parent!;

        // Write the content directory
        var byNames = CollectionsMarshal.AsSpan(ByNames);
        var byIds = CollectionsMarshal.AsSpan(ByIds);
        {
            using var tempSpan = TempSpan<byte>.Create((int)size, out var span);

            ref var rawResourceDirectory = ref Unsafe.As<byte, RawImageResourceDirectory>(ref MemoryMarshal.GetReference(span));

            rawResourceDirectory.Characteristics = Characteristics;
            rawResourceDirectory.TimeDateStamp = (uint)(TimeDateStamp - DateTime.UnixEpoch).TotalSeconds;
            rawResourceDirectory.MajorVersion = MajorVersion;
            rawResourceDirectory.MinorVersion = MinorVersion;
            rawResourceDirectory.NumberOfNamedEntries = (ushort)ByNames.Count;
            rawResourceDirectory.NumberOfIdEntries = (ushort)ByIds.Count;

            var rawEntries = MemoryMarshal.Cast<byte, RawImageResourceDirectoryEntry>(span.Slice(sizeof(RawImageResourceDirectory), (ByNames.Count + ByIds.Count) * sizeof(RawImageResourceDirectoryEntry)));

            var directoryPosition = directory.Position;

            for (int i = 0; i < byNames.Length; i++)
            {
                ref var rawEntry = ref rawEntries[i];
                var entry = byNames[i];

                rawEntry.NameOrId = IMAGE_RESOURCE_NAME_IS_STRING | (uint)(entry.Name.Position - directoryPosition);
                rawEntry.OffsetToDataOrDirectoryEntry = (uint)(entry.Entry.Position - directoryPosition);
            }

            for (int i = 0; i < byIds.Length; i++)
            {
                ref var rawEntry = ref rawEntries[byNames.Length + i];
                var entry = byIds[i];

                rawEntry.NameOrId = (uint)entry.Id.Value;
                rawEntry.OffsetToDataOrDirectoryEntry = IMAGE_RESOURCE_DATA_IS_DIRECTORY | (uint)(entry.Entry.Position - directoryPosition);
            }

            writer.Write(span);
        }
    }
    
    private void ReadEntry(in ReaderContext context, RawImageResourceDirectoryEntry rawEntry)
    {
        var directory = context.Directory;
        
        bool isDirectory = (rawEntry.OffsetToDataOrDirectoryEntry & IMAGE_RESOURCE_DATA_IS_DIRECTORY) != 0;
        var offset = rawEntry.OffsetToDataOrDirectoryEntry & ~IMAGE_RESOURCE_DATA_IS_DIRECTORY;

        PEResourceEntry entry = isDirectory ? new PEResourceDirectoryEntry() : new PEResourceDataEntry();
        entry.Position = directory.Position + offset;

        if ((rawEntry.NameOrId & IMAGE_RESOURCE_NAME_IS_STRING) != 0)
        {
            var resourceString = new PEResourceString()
            {
                Position = context.Directory.Position + (rawEntry.NameOrId & ~IMAGE_RESOURCE_NAME_IS_STRING)
            };
            
            ByNames.Add(new(resourceString, entry));
        }
        else
        {
            var id = (int)rawEntry.NameOrId;
            ByIds.Add(new(new(id), entry));
        }
    }

    public override unsafe void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = CalculateSize();
    }

    private unsafe uint CalculateSize()
    {
        var size = 0U;
        size += (uint)sizeof(RawImageResourceDirectory);
        size += (uint)(ByNames.Count + ByIds.Count) * (uint)sizeof(RawImageResourceDirectoryEntry);

        return size;
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"ByNames[{ByNames.Count}], ByIds[{ByIds.Count}] , TimeDateStamp = {TimeDateStamp}, MajorVersion = {MajorVersion}, MinorVersion = {MinorVersion}");

        return true;
    }
    
    private const uint IMAGE_RESOURCE_NAME_IS_STRING = 0x80000000;
    private const uint IMAGE_RESOURCE_DATA_IS_DIRECTORY = 0x80000000;
}
