// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace LibObjectFile.PE;

public abstract class PEDirectory : PESectionData
{
    protected PEDirectory(ImageDataDirectoryKind kind)
    {
        Kind = kind;
    }

    public ImageDataDirectoryKind Kind { get; }


    /// <summary>
    /// Factory method to create a new instance of <see cref="PEDirectory"/> based on the kind.
    /// </summary>
    /// <param name="kind">The kind of PE directory entry.</param>
    /// <returns>A PE directory entry.</returns>
    internal static PEDirectory Create(ImageDataDirectoryKind kind, RVALink<PESection> link)
    {
        return kind switch
        {
            ImageDataDirectoryKind.Export => new PEExportDirectory(),
            ImageDataDirectoryKind.Import => new PEImportDirectory(),
            ImageDataDirectoryKind.Resource => new PEResourceDirectory(),
            ImageDataDirectoryKind.Exception => new PEExceptionDirectory(),
            ImageDataDirectoryKind.Security => new PESecurityDirectory(),
            ImageDataDirectoryKind.BaseRelocation => new PEBaseRelocationDirectory(),
            ImageDataDirectoryKind.Debug => new PEDebugDirectory(),
            ImageDataDirectoryKind.Architecture => new PEArchitectureDirectory(),
            ImageDataDirectoryKind.GlobalPointer => new PEGlobalPointerDirectory(),
            ImageDataDirectoryKind.Tls => new PETlsDirectory(),
            ImageDataDirectoryKind.LoadConfig => new PELoadConfigDirectory(),
            ImageDataDirectoryKind.BoundImport => new PEBoundImportDirectory(),
            ImageDataDirectoryKind.DelayImport => new PEDelayImportDirectory(),
            ImageDataDirectoryKind.ImportAddressTable => new PEImportAddressTableDirectory(),
            ImageDataDirectoryKind.ClrMetadata => new PEClrMetadata(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    protected override void ValidateParent(ObjectFileNodeBase parent)
    {
        if (parent is not PESection)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }
}

public sealed class PEExportDirectory : PEDirectory
{
    public PEExportDirectory() : base(ImageDataDirectoryKind.Export)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEResourceDirectory : PEDirectory
{
    public PEResourceDirectory() : base(ImageDataDirectoryKind.Resource)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEExceptionDirectory : PEDirectory
{
    public PEExceptionDirectory() : base(ImageDataDirectoryKind.Exception)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDebugDirectory : PEDirectory
{
    public PEDebugDirectory() : base(ImageDataDirectoryKind.Debug)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PELoadConfigDirectory : PEDirectory
{
    public PELoadConfigDirectory() : base(ImageDataDirectoryKind.LoadConfig)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEBoundImportDirectory : PEDirectory
{
    public PEBoundImportDirectory() : base(ImageDataDirectoryKind.BoundImport)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PETlsDirectory : PEDirectory
{
    public PETlsDirectory() : base(ImageDataDirectoryKind.Tls)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDelayImportDirectory : PEDirectory
{
    public PEDelayImportDirectory() : base(ImageDataDirectoryKind.DelayImport)
    {
    }
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEClrMetadata : PEDirectory
{
    public PEClrMetadata() : base(ImageDataDirectoryKind.ClrMetadata)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEArchitectureDirectory : PEDirectory
{
    public PEArchitectureDirectory() : base(ImageDataDirectoryKind.Architecture)
    {
    }
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEGlobalPointerDirectory : PEDirectory
{
    public PEGlobalPointerDirectory() : base(ImageDataDirectoryKind.GlobalPointer)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PESecurityDirectory : PEDirectory
{
    public PESecurityDirectory() : base(ImageDataDirectoryKind.Security)
    {
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}
