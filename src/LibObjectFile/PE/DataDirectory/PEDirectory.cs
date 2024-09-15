// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using System;

namespace LibObjectFile.PE;

public abstract class PEDirectory(ImageDataDirectoryKind kind) : PESectionData
{
    public ImageDataDirectoryKind Kind { get; } = kind;
}

public sealed class PEExportDirectory() : PEDirectory(ImageDataDirectoryKind.Export)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEImportDirectory() : PEDirectory(ImageDataDirectoryKind.Import)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEResourceDirectory() : PEDirectory(ImageDataDirectoryKind.Resource)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEExceptionDirectory() : PEDirectory(ImageDataDirectoryKind.Exception)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEBaseRelocationDirectory() : PEDirectory(ImageDataDirectoryKind.BaseRelocation)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDebugDirectory() : PEDirectory(ImageDataDirectoryKind.Debug)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PELoadConfigDirectory() : PEDirectory(ImageDataDirectoryKind.LoadConfig)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEBoundImportDirectory() : PEDirectory(ImageDataDirectoryKind.BoundImport)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEImportAddressTable() : PEDirectory(ImageDataDirectoryKind.ImportAddressTable)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PETlsDirectory() : PEDirectory(ImageDataDirectoryKind.Tls)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEDelayImportDirectory() : PEDirectory(ImageDataDirectoryKind.DelayImport)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEClrMetadata() : PEDirectory(ImageDataDirectoryKind.ClrMetadata)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEArchitectureDirectory() : PEDirectory(ImageDataDirectoryKind.Architecture)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PEGlobalPointerDirectory() : PEDirectory(ImageDataDirectoryKind.GlobalPointer)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class PESecurityDirectory() : PEDirectory(ImageDataDirectoryKind.Security)
{
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        throw new NotImplementedException();
    }

    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}
