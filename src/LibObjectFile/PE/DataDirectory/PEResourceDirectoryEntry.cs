// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a directory entry in the Portable Executable (PE) <see cref="PEResourceDirectory"/>.
/// </summary>
/// <remarks>
/// This class provides functionality to manage a directory entry in the  <see cref="PEResourceDirectory"/>.
/// It allows adding, removing, and updating resource entries within the directory.
/// </remarks>
public sealed class PEResourceDirectoryEntry : PEResourceEntry, IEnumerable<PEResourceEntry>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<string, int> _nameToIndex = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<PEResourceId, int> _idToIndex = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDirectoryEntry"/> class.
    /// </summary>
    internal PEResourceDirectoryEntry()
    {
        Entries = new ObjectList<PEResourceEntry>(this, adding: AddingEntry, removing: RemovingEntry, updating: UpdatingEntry);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDirectoryEntry"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the resource directory entry.</param>
    public PEResourceDirectoryEntry(string name) : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Entries = new ObjectList<PEResourceEntry>(this, adding: AddingEntry, removing: RemovingEntry, updating: UpdatingEntry);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDirectoryEntry"/> class with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the resource directory entry.</param>
    public PEResourceDirectoryEntry(PEResourceId id) : base(id)
    {
        Entries = new ObjectList<PEResourceEntry>(this);
    }

    /// <summary>
    /// Gets or sets the time stamp of the resource directory entry.
    /// </summary>
    public DateTime TimeDateStamp { get; set; }

    /// <summary>
    /// Gets or sets the major version of the resource directory entry.
    /// </summary>
    public uint MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets the minor version of the resource directory entry.
    /// </summary>
    public uint MinorVersion { get; set; }

    /// <summary>
    /// Gets the list of resource entries within the directory.
    /// </summary>
    public ObjectList<PEResourceEntry> Entries { get; }

    /// <summary>
    /// Determines whether the directory contains a resource entry with the specified name.
    /// </summary>
    /// <param name="name">The name of the resource entry.</param>
    /// <returns><c>true</c> if the directory contains a resource entry with the specified name; otherwise, <c>false</c>.</returns>
    public bool Contains(string name) => _nameToIndex.ContainsKey(name);

    /// <summary>
    /// Determines whether the directory contains a resource entry with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the resource entry.</param>
    /// <returns><c>true</c> if the directory contains a resource entry with the specified ID; otherwise, <c>false</c>.</returns>
    public bool Contains(PEResourceId id) => _idToIndex.ContainsKey(id);

    /// <summary>
    /// Tries to get the resource entry with the specified name from the directory.
    /// </summary>
    /// <param name="name">The name of the resource entry.</param>
    /// <param name="entry">When this method returns, contains the resource entry with the specified name, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the resource entry with the specified name is found; otherwise, <c>false</c>.</returns>
    public bool TryGetEntry(string name, out PEResourceEntry? entry)
    {
        if (_nameToIndex.TryGetValue(name, out var index))
        {
            entry = Entries[index];
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>
    /// Tries to get the resource entry with the specified ID from the directory.
    /// </summary>
    /// <param name="id">The ID of the resource entry.</param>
    /// <param name="entry">When this method returns, contains the resource entry with the specified ID, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the resource entry with the specified ID is found; otherwise, <c>false</c>.</returns>
    public bool TryGetEntry(PEResourceId id, out PEResourceEntry? entry)
    {
        if (_idToIndex.TryGetValue(id, out var index))
        {
            entry = Entries[index];
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>
    /// Adds the specified resource entry to the directory.
    /// </summary>
    /// <param name="entry">The resource entry to add.</param>
    public void Add(PEResourceEntry entry) => Entries.Add(entry);

    /// <summary>
    /// Gets an enumerator that iterates through the resource entries in the directory.
    /// </summary>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public List<PEResourceEntry>.Enumerator GetEnumerator() => Entries.GetEnumerator();

    IEnumerator<PEResourceEntry> IEnumerable<PEResourceEntry>.GetEnumerator()
    {
        return Entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Entries).GetEnumerator();
    }

    private static void AddingEntry(ObjectElement parent, int index, PEResourceEntry entry)
    {
        var directory = (PEResourceDirectoryEntry)parent;
        if (entry.Name != null)
        {
            directory._nameToIndex.Add(entry.Name, index);
        }
        else
        {
            directory._idToIndex.Add(entry.Id, index);
        }
    }

    private static void RemovingEntry(ObjectElement parent, PEResourceEntry entry)
    {
        var directory = (PEResourceDirectoryEntry)parent;
        if (entry.Name != null)
        {
            directory._nameToIndex.Remove(entry.Name);
        }
        else
        {
            directory._idToIndex.Remove(entry.Id);
        }
    }

    private static void UpdatingEntry(ObjectElement parent, int index, PEResourceEntry previousEntry, PEResourceEntry entry)
    {
        RemovingEntry(parent, previousEntry);
        AddingEntry(parent, index, entry);
    }

    private protected override unsafe uint ComputeSize()
    {
        var entries = CollectionsMarshal.AsSpan(Entries.UnsafeList);
        uint size = (uint)sizeof(RawImageResourceDirectory);
        foreach (var entry in entries)
        {
            size += entry.ComputeFullSize();
        }

        return size;
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"Entries[{Entries.Count}] , TimeDateStamp = {TimeDateStamp}, MajorVersion = {MajorVersion}, MinorVersion = {MinorVersion}");

        return false;
    }
}
