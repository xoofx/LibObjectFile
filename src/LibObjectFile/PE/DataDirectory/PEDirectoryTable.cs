// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using LibObjectFile.Collections;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Contains the array of directory entries in a Portable Executable (PE) file.
/// </summary>
[DebuggerDisplay($"{nameof(PEDirectoryTable)} {nameof(Count)} = {{{nameof(Count)}}}")]
public sealed class PEDirectoryTable
{
    private PEObjectBase?[] _entries;
    private int _count;

    internal PEDirectoryTable()
    {
        _entries = [];
    }

    /// <summary>
    /// Gets the directory entry at the specified index.
    /// </summary>
    /// <param name="index">The index of the directory entry to get.</param>
    /// <returns>The directory entry at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
    public PEObjectBase? this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _entries[index];
        }

        set
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _entries[index] = value;
        }
    }

    /// <summary>
    /// Gets the directory entry of the specified kind. Must be within the bounds of <see cref="Count"/>.
    /// </summary>
    /// <param name="kind">The kind of directory entry to get.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the kind is out of range.</exception>
    public PEObjectBase? this[PEDataDirectoryKind kind]
    {
        get
        {
            int index = (int)(ushort)kind;
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return _entries[(int)kind];
        }

        set
        {
            int index = (int)(ushort)kind;
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            _entries[(int)kind] = value;
        }
    }

    /// <summary>
    /// Gets the maximum number of directory entries in the array.
    /// </summary>
    public int Count
    {
        get => _count;

        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);

            var previousCount = _count;
            // If the count is reduced, we need to check that all entries are null after
            for (int i = value; i < previousCount; i++)
            {
                if (_entries[i] is not null)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"A non null directory entry was found at index {i}. This directory entry must be removed before setting a count of {value}");
                }
            }

            if (_entries.Length < value)
            {
                Array.Resize(ref _entries, value);
            }
            
            _count = value;
        }
    }

    /// <summary>
    /// Gets the export directory information from the PE file.
    /// </summary>
    public PEExportDirectory? Export
    {
        get => (PEExportDirectory?)this[PEDataDirectoryKind.Export];
        set => this[PEDataDirectoryKind.Export] = value;
    }

    /// <summary>
    /// Gets the import directory information from the PE file.
    /// </summary>
    public PEImportDirectory? Import
    {
        get => (PEImportDirectory?)this[PEDataDirectoryKind.Import];
        set => this[PEDataDirectoryKind.Import] = value;
    }

    /// <summary>
    /// Gets the resource directory information from the PE file.
    /// </summary>
    public PEResourceDirectory? Resource
    {
        get => (PEResourceDirectory?)this[PEDataDirectoryKind.Resource];
        set => this[PEDataDirectoryKind.Resource] = value;
    }

    /// <summary>
    /// Gets the exception directory information from the PE file.
    /// </summary>
    public PEExceptionDirectory? Exception
    {
        get => (PEExceptionDirectory?)this[PEDataDirectoryKind.Exception];
        set => this[PEDataDirectoryKind.Exception] = value;
    }

    /// <summary>
    /// Gets the certificate/security directory information from the PE file.
    /// </summary>
    public PESecurityCertificateDirectory? Certificate
    {
        get => (PESecurityCertificateDirectory?)this[PEDataDirectoryKind.SecurityCertificate];
        set => this[PEDataDirectoryKind.SecurityCertificate] = value;
    }

    /// <summary>
    /// Gets the base relocation directory information from the PE file.
    /// </summary>
    public PEBaseRelocationDirectory? BaseRelocation
    {
        get => (PEBaseRelocationDirectory?)this[PEDataDirectoryKind.BaseRelocation];
        set => this[PEDataDirectoryKind.BaseRelocation] = value;
    }

    /// <summary>
    /// Gets the debug directory information from the PE file.
    /// </summary>
    public PEDebugDirectory? Debug
    {
        get => (PEDebugDirectory?)this[PEDataDirectoryKind.Debug];
        set => this[PEDataDirectoryKind.Debug] = value;
    }

    /// <summary>
    /// Gets the architecture-specific directory information from the PE file.
    /// </summary>
    public PEArchitectureDirectory? Architecture
    {
        get => (PEArchitectureDirectory?)this[PEDataDirectoryKind.Architecture];
        set => this[PEDataDirectoryKind.Architecture] = value;
    }

    /// <summary>
    /// Gets the global pointer directory information from the PE file.
    /// </summary>
    public PEGlobalPointerDirectory? GlobalPointer
    {
        get => (PEGlobalPointerDirectory?)this[PEDataDirectoryKind.GlobalPointer];
        set => this[PEDataDirectoryKind.GlobalPointer] = value;
    }

    /// <summary>
    /// Gets the TLS (Thread Local Storage) directory information from the PE file.
    /// </summary>
    public PETlsDirectory? Tls
    {
        get => (PETlsDirectory?)this[PEDataDirectoryKind.Tls];
        set => this[PEDataDirectoryKind.Tls] = value;
    }

    /// <summary>
    /// Gets the load configuration directory information from the PE file.
    /// </summary>
    public PELoadConfigDirectory? LoadConfig
    {
        get => (PELoadConfigDirectory?)this[PEDataDirectoryKind.LoadConfig];
        set => this[PEDataDirectoryKind.LoadConfig] = value;
    }

    /// <summary>
    /// Gets the bound import directory information from the PE file.
    /// </summary>
    public PEBoundImportDirectory? BoundImport
    {
        get => (PEBoundImportDirectory?)this[PEDataDirectoryKind.BoundImport];
        set => this[PEDataDirectoryKind.BoundImport] = value;
    }

    /// <summary>
    /// Gets the delay import directory information from the PE file.
    /// </summary>
    public PEDelayImportDirectory? DelayImport
    {
        get => (PEDelayImportDirectory?)this[PEDataDirectoryKind.DelayImport];
        set => this[PEDataDirectoryKind.DelayImport] = value;
    }

    /// <summary>
    /// Gets the import address table directory information from the PE file.
    /// </summary>
    public PEImportAddressTableDirectory? ImportAddressTableDirectory
    {
        get => (PEImportAddressTableDirectory?)this[PEDataDirectoryKind.ImportAddressTable];
        set => this[PEDataDirectoryKind.ImportAddressTable] = value;
    }

    /// <summary>
    /// Gets the CLR metadata directory information from the PE file, if present.
    /// </summary>
    public PEClrMetadata? ClrMetadata
    {
        get => (PEClrMetadata?)this[PEDataDirectoryKind.ClrMetadata];
        set => this[PEDataDirectoryKind.ClrMetadata] = value;
    }

    internal void Set(PESecurityCertificateDirectory? directory) => Set(PEDataDirectoryKind.SecurityCertificate, directory);

    internal void Set(PEDataDirectoryKind kind, PEObjectBase? directory) => Set((int)kind, directory);

    internal void Set(int index, PEObjectBase? directory)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"The directory entry only accepts {Count} entries. Set the count explicitly to allow more entries.");
        }

        _entries[index] = directory;
    }
    
    internal unsafe void Write(PEImageWriter writer, ref uint position)
    {
        using var tempSpan = TempSpan<RawImageDataDirectory>.Create(stackalloc byte[16 * sizeof(RawImageDataDirectory)], Count, out var span);
        span.Clear();

        for (int i = 0; i < Count; i++)
        {
            var entry = _entries[i];
            if (entry is not null)
            {
                ref var rawEntry = ref span[i];
                rawEntry.RVA = entry is PEDataDirectory dataDirectory ? dataDirectory.RVA : (uint)entry.Position;
                rawEntry.Size = (uint)entry.Size;
            }
        }

        writer.Write(tempSpan);
        
        position += (uint)(Count * sizeof(RawImageDataDirectory));
    }
}