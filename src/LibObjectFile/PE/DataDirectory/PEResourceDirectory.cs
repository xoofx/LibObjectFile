// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource directory in a Portable Executable (PE) file.
/// </summary>
public sealed class PEResourceDirectory : PEDataDirectory
{
    private List<PEResourceData>? _tempResourceDataList;


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

        _tempResourceDataList = new();
        var readerContext = new PEResourceEntry.ReaderContext(reader, this, _tempResourceDataList);
        Root.Read(readerContext);

        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override IEnumerable<PEObjectBase> CollectImplicitSectionDataList()
    {
        if (_tempResourceDataList is not null)
        {
            foreach (var data in _tempResourceDataList)
            {
                yield return data;
            }

            // We clear the list after being used - as this method is called once and we don't want to hold a reference
            _tempResourceDataList.Clear();
            _tempResourceDataList = null;
        }
    }
    
    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}
