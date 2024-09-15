// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for data contained in a <see cref="PESection"/>.
/// </summary>
public abstract class PESectionData : PEObject
{
    /// <summary>
    /// Gets the parent <see cref="PESection"/> of this section data.
    /// </summary>
    public PESection? Section
    {
        get => (PESection?)Parent;

        internal set => Parent = value;
    }

    protected abstract void WriteTo(Stream stream);
}

/// <summary>
/// Defines a raw section data in a Portable Executable (PE) image.
/// </summary>
public sealed class PESectionMemoryData : PESectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionMemoryData"/> class.
    /// </summary>
    public PESectionMemoryData() : this(Array.Empty<byte>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionMemoryData"/> class.
    /// </summary>
    /// <param name="data">The raw data.</param>
    public PESectionMemoryData(Memory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the raw data.
    /// </summary>
    public Memory<byte> Data { get; set; }

    /// <inheritdoc />
    public override ulong Size
    {
        get => (ulong)Data.Length;
        set => throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
    }

    protected override void WriteTo(Stream stream) => stream.Write(Data.Span);
}

/// <summary>
/// Gets a stream section data in a Portable Executable (PE) image.
/// </summary>
public sealed class PESectionStreamData : PESectionData
{
    private Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionStreamData"/> class.
    /// </summary>
    public PESectionStreamData()
    {
        _stream = Stream.Null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PESectionStreamData"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the data of this section data.</param>
    public PESectionStreamData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
    }

    /// <summary>
    /// Gets the stream containing the data of this section data.
    /// </summary>
    public Stream Stream
    {
        get => _stream;
        set => _stream = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override ulong Size
    {
        get => (ulong)Stream.Length;
        set => throw new InvalidOperationException();
    }

    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
    }

    protected override void WriteTo(Stream stream)
    {
        Stream.Position = 0;
        Stream.CopyTo(stream);
    }
}

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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
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

    protected override void WriteTo(Stream stream)
    {
        throw new NotImplementedException();
    }
}
