// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Utils;
using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource directory in a Portable Executable (PE) file.
/// </summary>
public sealed class PEResourceDirectory : PEDataDirectory
{
    private List<PEResourceString>? _tempResourceStrings;
    private List<PEResourceEntry>? _tempResourceEntries;
    
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
    protected override uint ComputeHeaderSize(PELayoutContext context)
    {
        Root.UpdateLayout(context);
        return (uint)Root.Size;
    }

    /// <inheritdoc/>
    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;

        Root.Position = Position;

        _tempResourceStrings = new();
        _tempResourceEntries = new();

        // Read the resource directory recursively
        var readerContext = new PEResourceEntry.ReaderContext(reader, this, _tempResourceStrings, _tempResourceEntries);
        Root.Read(readerContext);

        // Read the resource strings (that should follow the resource directory)
        foreach (var resourceString in _tempResourceStrings)
        {
            resourceString.Read(reader);
        }

        // Read the content of the resource data (that should usually be stored after the resource strings)
        bool isFirstDataEntry = true;
        foreach (var resourceEntry in _tempResourceEntries)
        {
            if (resourceEntry is not PEResourceDataEntry dataEntry)
            {
                continue;
            }

            var resourceData = dataEntry.Data;

            // If we find that the position is not aligned on 4 bytes as we expect, reset it to 1 byte alignment
            var checkPosition = AlignHelper.AlignUp(resourceData.Position, resourceData.RequiredPositionAlignment);
            if (checkPosition != resourceData.Position)
            {
                resourceData.RequiredPositionAlignment = 1;
            }
            else if (isFirstDataEntry && (resourceData.Position & 0xF) == 0)
            {
                // If we are the first resource data entry and the position is aligned on 16 bytes, we can assume this alignment
                resourceData.RequiredPositionAlignment = 16;
            }

            // Read the data
            resourceData.Read(reader);

            isFirstDataEntry = true;
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
    {
        if (_tempResourceStrings is not null)
        {
            foreach (var data in _tempResourceStrings)
            {
                yield return data;
            }

            // We clear the list after being used - as this method is called once and we don't want to hold a reference
            _tempResourceStrings.Clear();
            _tempResourceStrings = null;
        }

        if (_tempResourceEntries is not null)
        {
            foreach (var data in _tempResourceEntries)
            {
                yield return data;

                if (data is PEResourceDataEntry dataEntry)
                {
                    yield return dataEntry.Data;
                }
            }

            // We clear the list after being used - as this method is called once and we don't want to hold a reference
            _tempResourceEntries.Clear();
            _tempResourceEntries = null;
        }
    }
    
    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        Root.Write(writer);
    }
}
