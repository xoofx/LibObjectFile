// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

public abstract class PEDataDirectory : PECompositeSectionData
{
    protected PEDataDirectory(PEDataDirectoryKind kind)
    {
        Kind = kind;
    }

    public PEDataDirectoryKind Kind { get; }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PESection)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }

    /// <summary>
    /// Factory method to create a new instance of <see cref="PEDataDirectory"/> based on the kind.
    /// </summary>
    /// <param name="kind">The kind of PE directory entry.</param>
    /// <returns>A PE directory entry.</returns>
    internal static PEDataDirectory Create(PEDataDirectoryKind kind, bool is32Bits)
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
            PEDataDirectoryKind.Tls => is32Bits ? new PETlsDirectory32() : new PETlsDirectory64(),
            PEDataDirectoryKind.LoadConfig => is32Bits ? new PELoadConfigDirectory32() : new PELoadConfigDirectory64(),
            PEDataDirectoryKind.BoundImport => new PEBoundImportDirectory(),
            PEDataDirectoryKind.DelayImport => new PEDelayImportDirectory(),
            PEDataDirectoryKind.ImportAddressTable => new PEImportAddressTableDirectory(),
            PEDataDirectoryKind.ClrMetadata => new PEClrMetadata(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
