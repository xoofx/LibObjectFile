// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public abstract class PEDataDirectory : PESectionData
{
    protected PEDataDirectory(PEDataDirectoryKind kind) : base(true)
    {
        Kind = kind;
        Content = CreateObjectList<PESectionData>(this);
    }

    public PEDataDirectoryKind Kind { get; }

    internal uint HeaderSize { get; private protected set; }

    /// <summary>
    /// Gets the content of this directory.
    /// </summary>
    public ObjectList<PESectionData> Content { get; }

    public sealed override void UpdateLayout(PELayoutContext context)
    {
        var va = RVA;

        // We compute the size of the directory header
        // Each directory have a specific layout, so we delegate the computation to the derived class
        var headerSize = ComputeHeaderSize(context);
        HeaderSize = headerSize;
        va += headerSize;
        ulong size = headerSize;

        // A directory could have a content in addition to the header
        // So we update the VirtualAddress of each content and update the layout
        var position = Position;
        foreach (var table in Content)
        {
            table.RVA = va;

            // Update layout will update virtual address
            if (!context.UpdateSizeOnly)
            {
                table.Position = position;
            }

            table.UpdateLayout(context);
            
            va += (uint)table.Size;
            size += table.Size;
            position += table.Size;
        }

        Size = size;
    }

    internal virtual IEnumerable<PESectionData> CollectImplicitSectionDataList() => Enumerable.Empty<PESectionData>();

    internal virtual void Bind(PEImageReader reader)
    {
    }

    protected abstract uint ComputeHeaderSize(PEVisitorContext context);
    
    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PESection)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }

    protected override bool TryFindByRVAInChildren(RVA rva, out PEObject? result)
        => Content.TryFindByRVA(rva, true, out result);

    protected override void UpdateRVAInChildren()
    {
        var va = RVA;
        foreach (var table in Content)
        {
            table.UpdateRVA(va);
            va += (uint)table.Size;
        }
    }

    /// <summary>
    /// Factory method to create a new instance of <see cref="PEDataDirectory"/> based on the kind.
    /// </summary>
    /// <param name="kind">The kind of PE directory entry.</param>
    /// <returns>A PE directory entry.</returns>
    internal static PEDataDirectory Create(PEDataDirectoryKind kind)
    {
        return kind switch
        {
            PEDataDirectoryKind.Export => new PEExportDirectory(),
            PEDataDirectoryKind.Import => new PEImportDirectory(),
            PEDataDirectoryKind.Resource => new PEResourceDirectory(),
            PEDataDirectoryKind.Exception => new PEExceptionDirectory(),
            PEDataDirectoryKind.BaseRelocation => new PEBaseRelocationDirectory(),
            PEDataDirectoryKind.Debug => new PEDebugDirectory(),
            PEDataDirectoryKind.Architecture => new PEArchitectureDirectory(),
            PEDataDirectoryKind.GlobalPointer => new PEGlobalPointerDirectory(),
            PEDataDirectoryKind.Tls => new PETlsDirectory(),
            PEDataDirectoryKind.LoadConfig => new PELoadConfigDirectory(),
            PEDataDirectoryKind.BoundImport => new PEBoundImportDirectory(),
            PEDataDirectoryKind.DelayImport => new PEDelayImportDirectory(),
            PEDataDirectoryKind.ImportAddressTable => new PEImportAddressTableDirectory(),
            PEDataDirectoryKind.ClrMetadata => new PEClrMetadata(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
