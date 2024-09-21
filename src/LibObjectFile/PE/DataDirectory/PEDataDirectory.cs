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

    public sealed override void UpdateLayout(PEVisitorContext context)
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
        foreach (var table in Content)
        {
            table.RVA = va;
            
            // Update layout will update virtual address
            table.UpdateLayout(context);

            va += (uint)table.Size;
            size += table.Size;
        }

        Size = size;
    }

    internal virtual IEnumerable<PESectionData> CollectImplicitSectionDataList() => Enumerable.Empty<PESectionData>();

    internal virtual void Bind(PEImageReader reader)
    {
    }

    protected abstract uint ComputeHeaderSize(PEVisitorContext context);
    
    protected override void ValidateParent(ObjectFileElement parent)
    {
        if (parent is not PESection)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }

    protected override bool TryFindByVirtualAddressInChildren(RVA rva, out PEVirtualObject? result)
        => Content.TryFindByVirtualAddress(rva, true, out result);

    protected override void UpdateVirtualAddressInChildren()
    {
        var va = RVA;
        foreach (var table in Content)
        {
            table.UpdateVirtualAddress(va);
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
            PEDataDirectoryKind.Security => new PESecurityDirectory(),
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

public sealed class PEResourceDirectory : PEDataDirectory
{
    public PEResourceDirectory() : base(PEDataDirectoryKind.Resource)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEExceptionDirectory : PEDataDirectory
{
    public PEExceptionDirectory() : base(PEDataDirectoryKind.Exception)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }


    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDebugDirectory : PEDataDirectory
{
    public PEDebugDirectory() : base(PEDataDirectoryKind.Debug)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }


    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PELoadConfigDirectory : PEDataDirectory
{
    public PELoadConfigDirectory() : base(PEDataDirectoryKind.LoadConfig)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }


    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEBoundImportDirectory : PEDataDirectory
{
    public PEBoundImportDirectory() : base(PEDataDirectoryKind.BoundImport)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PETlsDirectory : PEDataDirectory
{
    public PETlsDirectory() : base(PEDataDirectoryKind.Tls)
    {
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }
    
    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDelayImportDirectory : PEDataDirectory
{
    public PEDelayImportDirectory() : base(PEDataDirectoryKind.DelayImport)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEClrMetadata : PEDataDirectory
{
    public PEClrMetadata() : base(PEDataDirectoryKind.ClrMetadata)
    {
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEArchitectureDirectory : PEDataDirectory
{
    public PEArchitectureDirectory() : base(PEDataDirectoryKind.Architecture)
    {
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEGlobalPointerDirectory : PEDataDirectory
{
    public PEGlobalPointerDirectory() : base(PEDataDirectoryKind.GlobalPointer)
    {
    }
    
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PESecurityDirectory : PEDataDirectory
{
    public PESecurityDirectory() : base(PEDataDirectoryKind.Security)
    {
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}
