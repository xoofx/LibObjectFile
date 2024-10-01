// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using LibObjectFile.Collections;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Contains the array of directory entries in a Portable Executable (PE) file.
/// </summary>
/// <remarks>
/// The list of directory is automatically updated by the content of the sections
/// when calling <see cref="PEFile.Read"/> or <see cref="PEFile.Write"/>
/// or <see cref="PEFile.UpdateDirectories"/>
/// or <see cref="PEFile.UpdateLayout(LibObjectFile.Diagnostics.DiagnosticBag)"/>
/// </remarks>
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
    /// Clear the directory
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entries);
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

        internal set
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

        internal set
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
    }

    /// <summary>
    /// Gets the import directory information from the PE file.
    /// </summary>
    public PEImportDirectory? Import
    {
        get => (PEImportDirectory?)this[PEDataDirectoryKind.Import];
    }

    /// <summary>
    /// Gets the resource directory information from the PE file.
    /// </summary>
    public PEResourceDirectory? Resource
    {
        get => (PEResourceDirectory?)this[PEDataDirectoryKind.Resource];
    }

    /// <summary>
    /// Gets the exception directory information from the PE file.
    /// </summary>
    public PEExceptionDirectory? Exception
    {
        get => (PEExceptionDirectory?)this[PEDataDirectoryKind.Exception];
    }

    /// <summary>
    /// Gets the certificate/security directory information from the PE file.
    /// </summary>
    public PESecurityCertificateDirectory? Certificate
    {
        get => (PESecurityCertificateDirectory?)this[PEDataDirectoryKind.SecurityCertificate];
    }

    /// <summary>
    /// Gets the base relocation directory information from the PE file.
    /// </summary>
    public PEBaseRelocationDirectory? BaseRelocation
    {
        get => (PEBaseRelocationDirectory?)this[PEDataDirectoryKind.BaseRelocation];
    }

    /// <summary>
    /// Gets the debug directory information from the PE file.
    /// </summary>
    public PEDebugDirectory? Debug
    {
        get => (PEDebugDirectory?)this[PEDataDirectoryKind.Debug];
    }

    /// <summary>
    /// Gets the architecture-specific directory information from the PE file.
    /// </summary>
    public PEArchitectureDirectory? Architecture
    {
        get => (PEArchitectureDirectory?)this[PEDataDirectoryKind.Architecture];
    }

    /// <summary>
    /// Gets the global pointer directory information from the PE file.
    /// </summary>
    public PEGlobalPointerDirectory? GlobalPointer
    {
        get => (PEGlobalPointerDirectory?)this[PEDataDirectoryKind.GlobalPointer];
    }

    /// <summary>
    /// Gets the TLS (Thread Local Storage) directory information from the PE file.
    /// </summary>
    public PETlsDirectory? Tls
    {
        get => (PETlsDirectory?)this[PEDataDirectoryKind.Tls];
    }

    /// <summary>
    /// Gets the load configuration directory information from the PE file.
    /// </summary>
    public PELoadConfigDirectory? LoadConfig
    {
        get => (PELoadConfigDirectory?)this[PEDataDirectoryKind.LoadConfig];
    }

    /// <summary>
    /// Gets the bound import directory information from the PE file.
    /// </summary>
    public PEBoundImportDirectory? BoundImport
    {
        get => (PEBoundImportDirectory?)this[PEDataDirectoryKind.BoundImport];
    }

    /// <summary>
    /// Gets the delay import directory information from the PE file.
    /// </summary>
    public PEDelayImportDirectory? DelayImport
    {
        get => (PEDelayImportDirectory?)this[PEDataDirectoryKind.DelayImport];
    }

    /// <summary>
    /// Gets the import address table directory information from the PE file.
    /// </summary>
    public PEImportAddressTableDirectory? ImportAddressTableDirectory
    {
        get => (PEImportAddressTableDirectory?)this[PEDataDirectoryKind.ImportAddressTable];
    }

    /// <summary>
    /// Gets the CLR metadata directory information from the PE file, if present.
    /// </summary>
    public PEClrMetadata? ClrMetadata
    {
        get => (PEClrMetadata?)this[PEDataDirectoryKind.ClrMetadata];
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