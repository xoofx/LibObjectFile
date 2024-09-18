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
public sealed class PEDirectoryTable : IEnumerable<PEDirectory>
{
    private InternalArray _entries;
    private int _count;

    internal PEDirectoryTable()
    {
    }

    public PEDirectory? this[ImageDataDirectoryKind kind] => _entries[(int)kind];

    /// <summary>
    /// Gets the number of directory entries in the array.
    /// </summary>
    public int Count => _count;
    
    /// <summary>
    /// Gets the export directory information from the PE file.
    /// </summary>
    public PEExportDirectory? Export => (PEExportDirectory?)this[ImageDataDirectoryKind.Export];

    /// <summary>
    /// Gets the import directory information from the PE file.
    /// </summary>
    public PEImportDirectory? Import => (PEImportDirectory?)this[ImageDataDirectoryKind.Import];

    /// <summary>
    /// Gets the resource directory information from the PE file.
    /// </summary>
    public PEResourceDirectory? Resource => (PEResourceDirectory?)this[ImageDataDirectoryKind.Resource];

    /// <summary>
    /// Gets the exception directory information from the PE file.
    /// </summary>
    public PEExceptionDirectory? Exception => (PEExceptionDirectory?)this[ImageDataDirectoryKind.Exception];

    /// <summary>
    /// Gets the security directory information from the PE file.
    /// </summary>
    public PESecurityDirectory? Security => (PESecurityDirectory?)this[ImageDataDirectoryKind.Security];

    /// <summary>
    /// Gets the base relocation directory information from the PE file.
    /// </summary>
    public PEBaseRelocationDirectory? BaseRelocation => (PEBaseRelocationDirectory?)this[ImageDataDirectoryKind.BaseRelocation];

    /// <summary>
    /// Gets the debug directory information from the PE file.
    /// </summary>
    public PEDebugDirectory? Debug => (PEDebugDirectory?)this[ImageDataDirectoryKind.Debug];

    /// <summary>
    /// Gets the architecture-specific directory information from the PE file.
    /// </summary>
    public PEArchitectureDirectory? Architecture => (PEArchitectureDirectory?)this[ImageDataDirectoryKind.Architecture];

    /// <summary>
    /// Gets the global pointer directory information from the PE file.
    /// </summary>
    public PEGlobalPointerDirectory? GlobalPointer => (PEGlobalPointerDirectory?)this[ImageDataDirectoryKind.GlobalPointer];

    /// <summary>
    /// Gets the TLS (Thread Local Storage) directory information from the PE file.
    /// </summary>
    public PETlsDirectory? Tls => (PETlsDirectory?)this[ImageDataDirectoryKind.Tls];

    /// <summary>
    /// Gets the load configuration directory information from the PE file.
    /// </summary>
    public PELoadConfigDirectory? LoadConfig => (PELoadConfigDirectory?)this[ImageDataDirectoryKind.LoadConfig];

    /// <summary>
    /// Gets the bound import directory information from the PE file.
    /// </summary>
    public PEBoundImportDirectory? BoundImport => (PEBoundImportDirectory?)this[ImageDataDirectoryKind.BoundImport];

    /// <summary>
    /// Gets the delay import directory information from the PE file.
    /// </summary>
    public PEDelayImportDirectory? DelayImport => (PEDelayImportDirectory?)this[ImageDataDirectoryKind.DelayImport];

    /// <summary>
    /// Gets the import address table directory information from the PE file.
    /// </summary>
    public PEImportAddressTableDirectory? ImportAddressTable => (PEImportAddressTableDirectory?)this[ImageDataDirectoryKind.ImportAddressTable];

    /// <summary>
    /// Gets the CLR metadata directory information from the PE file, if present.
    /// </summary>
    public PEClrMetadata? ClrMetadata => (PEClrMetadata?)this[ImageDataDirectoryKind.ClrMetadata];

    /// <summary>
    /// Gets the enumerator for the directory entries.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Enumerator GetEnumerator() => new(this);
    
    internal void Set(ImageDataDirectoryKind kind, PEDirectory? directory)
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
    
    [InlineArray(15)]
    private struct InternalArray
    {
        private PEDirectory? _element;
    }

    /// <summary>
    /// Enumerator for the directory entries.
    /// </summary>
    public struct Enumerator : IEnumerator<PEDirectory>
    {
        private readonly PEDirectoryTable _table;
        private int _index;

        internal Enumerator(PEDirectoryTable table)
        {
            _table = table;
            _index = -1;
        }

        public PEDirectory Current => _index >= 0 ? _table._entries[_index]! : null!;

        object? IEnumerator.Current => Current;


        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            Span<PEDirectory?> entries = _table._entries;
            while (++_index < entries.Length)
            {
                if (_table._entries[_index] is not null)
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

    IEnumerator<PEDirectory> IEnumerable<PEDirectory>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}