// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource directory in a Portable Executable (PE) file.
/// </summary>
public sealed class PEResourceDirectory : PEDataDirectory, IEnumerable<PEResourceEntry>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDirectory"/> class.
    /// </summary>
    public PEResourceDirectory() : base(PEDataDirectoryKind.Resource)
    {
        Root = new()
        {
            Parent = this
        };
    }

    /// <summary>
    /// Gets the root resource directory entry.
    /// </summary>
    public PEResourceDirectoryEntry Root { get; }

    /// <inheritdoc/>
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        var size = Root.ComputeFullSize();
        size = (uint)AlignHelper.AlignUp(size, 4);
        return size;
    }

    /// <inheritdoc/>
    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var currentDirectory = Root;
        ReadDirectory(reader, currentDirectory);

        HeaderSize = ComputeHeaderSize(reader);
    }

    private unsafe void ReadDirectory(PEImageReader reader, PEResourceDirectoryEntry currentDirectory)
    {
        if (!reader.TryReadData<RawImageResourceDirectory>(sizeof(RawImageResourceDirectory), out var data))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory at position {reader.Position}");
            return;
        }

        currentDirectory.TimeDateStamp = DateTime.UnixEpoch.AddSeconds(data.TimeDateStamp);
        currentDirectory.MajorVersion = data.MajorVersion;
        currentDirectory.MinorVersion = data.MinorVersion;

        var buffer = new byte[(data.NumberOfNamedEntries + data.NumberOfIdEntries) * sizeof(RawImageResourceDirectoryEntry)];
        var spanEntries = MemoryMarshal.Cast<byte, RawImageResourceDirectoryEntry>(buffer);

        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory at position {reader.Position}");
            return;
        }

        for (int i = 0; i < data.NumberOfNamedEntries + data.NumberOfIdEntries; i++)
        {
            var entry = spanEntries[i];
            ReadEntry(reader, currentDirectory, entry);

            if (reader.Diagnostics.HasErrors)
            {
                return;
            }
        }
    }

    private unsafe void ReadEntry(PEImageReader reader, PEResourceDirectoryEntry parent, RawImageResourceDirectoryEntry rawEntry)
    {
        string? name = null;
        int id = 0;

        if ((rawEntry.NameOrId & IMAGE_RESOURCE_NAME_IS_STRING) != 0)
        {
            // Read the string
            var length = reader.ReadU16();
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                int readLength = reader.Read(buffer, 0, length);
                if (readLength != length)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource directory string at position {reader.Position}");
                    return;
                }
                name = Encoding.Unicode.GetString(buffer, 0, readLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            id = (int)(rawEntry.NameOrId & ~IMAGE_RESOURCE_NAME_IS_STRING);
        }


        bool isDirectory = (rawEntry.OffsetToDataOrDirectoryEntry & IMAGE_RESOURCE_DATA_IS_DIRECTORY) != 0;
        var offset = rawEntry.OffsetToDataOrDirectoryEntry & ~IMAGE_RESOURCE_DATA_IS_DIRECTORY;
        PEResourceEntry entry = isDirectory
            ? (name is null) ? new PEResourceDirectoryEntry(new PEResourceId(id)) : new PEResourceDirectoryEntry(name)
            : (name is null) ? new PEResourceDataEntry(new PEResourceId(id)) : new PEResourceDataEntry(name);

        parent.Entries.Add(entry);

        reader.Position = Position + offset;

        if (isDirectory)
        {
            var directory = (PEResourceDirectoryEntry)entry;
            ReadDirectory(reader, directory);
        }
        else
        {
            var dataEntry = (PEResourceDataEntry)entry;
            RawImageResourceDataEntry rawDataEntry;
            if (!reader.TryReadData(sizeof(RawImageResourceDataEntry), out rawDataEntry))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource data entry at position {reader.Position}");
                return;
            }

            dataEntry.CodePage = rawDataEntry.CodePage != 0 ? Encoding.GetEncoding((int)rawDataEntry.CodePage) : null;

            if (!reader.File.TryFindSection(rawDataEntry.OffsetToData, out var section))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntryRVAOffsetToData, $"Invalid resource data entry at position {reader.Position}. The RVA {rawDataEntry.OffsetToData} does not map to an existing section.");
                return;
            }

            var position = section.Position + rawDataEntry.OffsetToData - section.RVA;
            reader.Position = position;

            if (dataEntry.CodePage != null)
            {
                var buffer = ArrayPool<byte>.Shared.Rent((int)rawDataEntry.Size);
                try
                {
                    int read = reader.Read(buffer, 0, (int)rawDataEntry.Size);
                    if (read != rawDataEntry.Size)
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource data entry at position {reader.Position}");
                        return;
                    }
                    dataEntry.Data = dataEntry.CodePage.GetString(buffer, 0, (int)rawDataEntry.Size);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                dataEntry.Data = reader.ReadAsStream(rawDataEntry.Size);
            }
        }
    }

    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public List<PEResourceEntry>.Enumerator GetEnumerator() => Root.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<PEResourceEntry> IEnumerable<PEResourceEntry>.GetEnumerator()
    {
        return Root.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Root).GetEnumerator();
    }

    private const uint IMAGE_RESOURCE_NAME_IS_STRING = 0x80000000;
    private const uint IMAGE_RESOURCE_DATA_IS_DIRECTORY = 0x80000000;
}
