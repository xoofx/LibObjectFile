// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

public abstract class PEDataDirectory : PESectionData
{
    protected PEDataDirectory(PEDataDirectoryKind kind, bool hasChildren) : base(hasChildren)
    {
        Kind = kind;
    }

    public PEDataDirectoryKind Kind { get; }


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

    protected override void ValidateParent(ObjectFileElement parent)
    {
        if (parent is not PESection)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }
}

public sealed class PEExportDirectory : PEDataDirectory
{
    public PEExportDirectory() : base(PEDataDirectoryKind.Export, true)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEResourceDirectory : PEDataDirectory
{
    public PEResourceDirectory() : base(PEDataDirectoryKind.Resource, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEExceptionDirectory : PEDataDirectory
{
    public PEExceptionDirectory() : base(PEDataDirectoryKind.Exception, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDebugDirectory : PEDataDirectory
{
    public PEDebugDirectory() : base(PEDataDirectoryKind.Debug, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PELoadConfigDirectory : PEDataDirectory
{
    public PELoadConfigDirectory() : base(PEDataDirectoryKind.LoadConfig, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEBoundImportDirectory : PEDataDirectory
{
    public PEBoundImportDirectory() : base(PEDataDirectoryKind.BoundImport, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PETlsDirectory : PEDataDirectory
{
    public PETlsDirectory() : base(PEDataDirectoryKind.Tls, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDelayImportDirectory : PEDataDirectory
{
    public PEDelayImportDirectory() : base(PEDataDirectoryKind.DelayImport, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEClrMetadata : PEDataDirectory
{
    public PEClrMetadata() : base(PEDataDirectoryKind.ClrMetadata, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEArchitectureDirectory : PEDataDirectory
{
    public PEArchitectureDirectory() : base(PEDataDirectoryKind.Architecture, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEGlobalPointerDirectory : PEDataDirectory
{
    public PEGlobalPointerDirectory() : base(PEDataDirectoryKind.GlobalPointer, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PESecurityDirectory : PEDataDirectory
{
    public PESecurityDirectory() : base(PEDataDirectoryKind.Security, false)
    {
    }

    public override void UpdateLayout(PEVisitorContext context)
    {
        throw new NotImplementedException();
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}
