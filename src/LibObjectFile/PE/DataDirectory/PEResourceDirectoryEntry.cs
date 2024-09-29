// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
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
    /// Gets or sets the time stamp of the resource directory entry.
    /// </summary>
    public DateTime TimeDateStamp { get; set; }

    /// <summary>
    /// Gets or sets the major version of the resource directory entry.
    /// </summary>
    public uint MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets the minor version of the resource directory entry.
    /// </summary>
    public uint MinorVersion { get; set; }

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
        var directory = context.Directory;
        
        reader.Position = Position;
        if (!reader.TryReadData<RawImageResourceDirectory>(sizeof(RawImageResourceDirectory), out var data))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory at position {reader.Position}");
            return;
        }

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
            ReadEntry(reader, directory, entry);

            if (reader.Diagnostics.HasErrors)
            {
                return;
            }
        }

        // Update the size
        Size = reader.Position - Position;

        var size = CalculateSize();
        if (Size != size)
        {

        }


        // Process all the entries recursively
        var byNames = CollectionsMarshal.AsSpan(ByNames);
        foreach (ref var entry in byNames)
        {
            entry.Entry.Read(context);
        }

        var byIds = CollectionsMarshal.AsSpan(ByIds);
        foreach (ref var entry in byIds)
        {
            entry.Entry.Read(context);
        }
    }

    private void ReadEntry(PEImageReader reader, PEResourceDirectory directory, RawImageResourceDirectoryEntry rawEntry)
    {
        string? name = null;
        int id = 0;

        if ((rawEntry.NameOrId & IMAGE_RESOURCE_NAME_IS_STRING) != 0)
        {
            // Read the string
            var length = reader.ReadU16() * 2;
            using var pooledSpan = PooledSpan<byte>.Create(length, out var span);

            int readLength = reader.Read(span);
            if (readLength != length)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory string at position {reader.Position}");
                return;
            }
            name = Encoding.Unicode.GetString(span);
        }
        else
        {
            id = (int)(rawEntry.NameOrId & ~IMAGE_RESOURCE_NAME_IS_STRING);
        }
        
        bool isDirectory = (rawEntry.OffsetToDataOrDirectoryEntry & IMAGE_RESOURCE_DATA_IS_DIRECTORY) != 0;
        var offset = rawEntry.OffsetToDataOrDirectoryEntry & ~IMAGE_RESOURCE_DATA_IS_DIRECTORY;

        PEResourceEntry entry = isDirectory ? new PEResourceDirectoryEntry() : new PEResourceDataEntry();
        entry.Position = directory.Position + offset;

        if (name is not null)
        {
            ByNames.Add(new(name, entry));
        }
        else
        {
            ByIds.Add(new(new(id), entry));
        }

        // Add the content to the directory (as we have the guarantee that the content belongs to the resource directory)
        directory.Content.Add(entry);
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

        if (ByNames.Count > 0)
        {
            var byNames = CollectionsMarshal.AsSpan(ByNames);
            foreach (ref readonly var entry in byNames)
            {
                size += sizeof(ushort) + (uint)entry.Name.Length * 2;
            }
        }

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