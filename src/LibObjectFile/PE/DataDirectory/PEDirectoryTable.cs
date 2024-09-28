// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LibObjectFile.PE;

/// <summary>
/// Contains the array of directory entries in a Portable Executable (PE) file.
/// </summary>
[DebuggerDisplay($"{nameof(PEDirectoryTable)} {nameof(Count)} = {{{nameof(Count)}}}")]
public sealed class PEDirectoryTable : IEnumerable<PEDataDirectory>
{
    private InternalArray _entries;
    private int _count;

    internal PEDirectoryTable()
    {
    }

    public PEObjectBase? this[PEDataDirectoryKind kind] => _entries[(int)kind];

    /// <summary>
    /// Gets the number of directory entries in the array.
    /// </summary>
    public int Count => _count;
    
    /// <summary>
    /// Gets the export directory information from the PE file.
    /// </summary>
    public PEExportDirectory? Export => (PEExportDirectory?)this[PEDataDirectoryKind.Export];

    /// <summary>
    /// Gets the import directory information from the PE file.
    /// </summary>
    public PEImportDirectory? Import => (PEImportDirectory?)this[PEDataDirectoryKind.Import];

    /// <summary>
    /// Gets the resource directory information from the PE file.
    /// </summary>
    public PEResourceDirectory? Resource => (PEResourceDirectory?)this[PEDataDirectoryKind.Resource];

    /// <summary>
    /// Gets the exception directory information from the PE file.
    /// </summary>
    public PEExceptionDirectory? Exception => (PEExceptionDirectory?)this[PEDataDirectoryKind.Exception];

    /// <summary>
    /// Gets the certificate/security directory information from the PE file.
    /// </summary>
    public PESecurityCertificateDirectory? Certificate => (PESecurityCertificateDirectory?)this[PEDataDirectoryKind.SecurityCertificate];

    /// <summary>
    /// Gets the base relocation directory information from the PE file.
    /// </summary>
    public PEBaseRelocationDirectory? BaseRelocation => (PEBaseRelocationDirectory?)this[PEDataDirectoryKind.BaseRelocation];

    /// <summary>
    /// Gets the debug directory information from the PE file.
    /// </summary>
    public PEDebugDirectory? Debug => (PEDebugDirectory?)this[PEDataDirectoryKind.Debug];

    /// <summary>
    /// Gets the architecture-specific directory information from the PE file.
    /// </summary>
    public PEArchitectureDirectory? Architecture => (PEArchitectureDirectory?)this[PEDataDirectoryKind.Architecture];

    /// <summary>
    /// Gets the global pointer directory information from the PE file.
    /// </summary>
    public PEGlobalPointerDirectory? GlobalPointer => (PEGlobalPointerDirectory?)this[PEDataDirectoryKind.GlobalPointer];

    /// <summary>
    /// Gets the TLS (Thread Local Storage) directory information from the PE file.
    /// </summary>
    public PETlsDirectory? Tls => (PETlsDirectory?)this[PEDataDirectoryKind.Tls];

    /// <summary>
    /// Gets the load configuration directory information from the PE file.
    /// </summary>
    public PELoadConfigDirectory? LoadConfig => (PELoadConfigDirectory?)this[PEDataDirectoryKind.LoadConfig];

    /// <summary>
    /// Gets the bound import directory information from the PE file.
    /// </summary>
    public PEBoundImportDirectory? BoundImport => (PEBoundImportDirectory?)this[PEDataDirectoryKind.BoundImport];

    /// <summary>
    /// Gets the delay import directory information from the PE file.
    /// </summary>
    public PEDelayImportDirectory? DelayImport => (PEDelayImportDirectory?)this[PEDataDirectoryKind.DelayImport];

    /// <summary>
    /// Gets the import address table directory information from the PE file.
    /// </summary>
    public PEImportAddressTableDirectory? ImportAddressTableDirectory => (PEImportAddressTableDirectory?)this[PEDataDirectoryKind.ImportAddressTable];

    /// <summary>
    /// Gets the CLR metadata directory information from the PE file, if present.
    /// </summary>
    public PEClrMetadata? ClrMetadata => (PEClrMetadata?)this[PEDataDirectoryKind.ClrMetadata];

    /// <summary>
    /// Gets the enumerator for the directory entries.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Enumerator GetEnumerator() => new(this);

    internal void Set(PESecurityCertificateDirectory? directory)
    {
        var kind = PEDataDirectoryKind.SecurityCertificate;
        ref var entry = ref _entries[(int)kind];
        var previousEntry = entry;
        entry = directory;

        if (previousEntry is not null)
        {
            _count--;
        }

        if (directory is not null)
        {
            _count++;
        }
    }
    
    internal void Set(PEDataDirectoryKind kind, PEDataDirectory? directory)
    {
        ref var entry = ref _entries[(int)kind];
        var previousEntry = entry;
        entry = directory;
        
        if (previousEntry is not null)
        {
            _count--;
        }

        if (directory is not null)
        {
            _count++;
        }
    }

    internal int CalculateNumberOfEntries()
    {
        int count = 0;
        ReadOnlySpan<PEObjectBase?> span = _entries;
        for(int i = 0; i < span.Length; i++) 
        {
            if (_entries[i] is not null)
            {
                count = i + 1;
            }
        }

        return count;
    }

    internal unsafe void Write(PEImageWriter writer, ref uint position)
    {
        var numberOfEntries = CalculateNumberOfEntries();
        for (int i = 0; i < numberOfEntries; i++)
        {
            ImageDataDirectory rawDataDirectory = default;
            var entry = _entries[i];
            if (entry is not null)
            {
                rawDataDirectory.RVA = entry is PEDataDirectory dataDirectory ? dataDirectory.RVA : (uint)entry.Position;
                rawDataDirectory.Size = (uint)entry.Size;
            }
        }

        position += (uint)(numberOfEntries * sizeof(ImageDataDirectory));
    }
    
    [InlineArray(15)]
    private struct InternalArray
    {
        private PEObjectBase? _element;
    }

    /// <summary>
    /// Enumerator for the directory entries.
    /// </summary>
    public struct Enumerator : IEnumerator<PEDataDirectory>
    {
        private readonly PEDirectoryTable _table;
        private int _index;

        internal Enumerator(PEDirectoryTable table)
        {
            _table = table;
            _index = -1;
        }

        public PEDataDirectory Current => _index >= 0 ? (PEDataDirectory)_table._entries[_index]! : null!;

        object? IEnumerator.Current => Current;


        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            Span<PEObjectBase?> entries = _table._entries;
            while (++_index < entries.Length)
            {
                if (_table._entries[_index] is PEDataDirectory)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    IEnumerator<PEDataDirectory> IEnumerable<PEDataDirectory>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}