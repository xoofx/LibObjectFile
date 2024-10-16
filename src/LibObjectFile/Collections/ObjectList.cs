// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LibObjectFile.Collections;

/// <summary>
/// A list of objects that are attached to a parent object.
/// </summary>
/// <typeparam name="TObject">The type of the object file.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ObjectList<>.ObjectListDebuggerView))]
public readonly struct ObjectList<TObject> : IList<TObject>
    where TObject : ObjectElement
{
    // We are using an internal list to keep track of the parent object
    private readonly InternalList _items;

    [Obsolete("This constructor is not supported", true)]
    public ObjectList() => throw new NotSupportedException("This constructor is not supported");

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectList{TObject}"/> class.
    /// </summary>
    /// <param name="parent">The parent object file node.</param>
    public ObjectList(
        ObjectElement parent,
        Action<ObjectElement, int, TObject>? adding= null,
        Action<ObjectElement, TObject>? added = null,
        Action<ObjectElement, TObject>? removing = null,
        Action<ObjectElement, int, TObject>? removed = null,
        Action<ObjectElement, int, TObject, TObject>? updating = null,
        Action<ObjectElement, int, TObject, TObject>? updated = null
        )
    {
        ArgumentNullException.ThrowIfNull(parent);
        _items = new InternalList(parent, adding, added, removing, removed, updating, updated);
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public List<TObject> UnsafeList => _items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TObject item)
    {
        CheckAdd(item);
        var items = _items;
        int index = items.Count;
        items.Adding(index, item);
        items.Add(AssignAdd(item));
        item.Index = index;
        items.Added(item);
    }

    public void Clear()
    {
        var items = _items;
        for (var i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            items.Removing(item);
            items.RemoveAt(i);
            item.Parent = null;
            item.ResetIndex();
            items.Removed(i, item);
        }

        items.Clear();
    }

    public bool Contains(TObject item) => _items.Contains(item);

    public void CopyTo(TObject[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public bool Remove(TObject item)
    {
        var items = _items;
        if (item.Parent != items.Parent)
        {
            return false;
        }

        item.Parent = null;
        item.ResetIndex();

        return items.Remove(item);
    }

    public int IndexOf(TObject item) => _items.IndexOf(item);

    public void Insert(int index, TObject item)
    {
        if ((uint)index > (uint)_items.Count) throw new ArgumentOutOfRangeException(nameof(index));

        CheckAdd(item);
        var items = _items;
        items.Adding(index, item);
        items.Insert(index, AssignAdd(item));

        for (int i = index; i < items.Count; i++)
        {
            items[i].Index = i;
        }

        items.Added(item);
    }

    public void RemoveAt(int index)
    {
        var items = _items;
        var item = items[index];
        items.Removing(item);
        item.Parent = null;
        item.ResetIndex();

        items.RemoveAt(index);

        for (int i = index; i < items.Count; i++)
        {
            items[i].Index = i;
        }
        items.Removed(index, item);
    }

    public TObject this[int index]
    {
        get => _items[index];
        set
        {
            CheckAdd(value);

            // Unbind previous entry
            var items = _items;
            var previousItem = items[index];
            items.Updating(index, previousItem, value);
            items.Removing(previousItem);
            previousItem.Parent = null;
            previousItem.ResetIndex();

            // Bind new entry
            items[index] = AssignAdd(value);
            value.Index = index;
            items.Updated(index, previousItem, value);
        }
    }

    public List<TObject>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<TObject> IEnumerable<TObject>.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }

    private void CheckAdd(TObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Parent != null)
        {
            throw new ArgumentException($"The object is already attached to another parent", nameof(item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TObject AssignAdd(TObject item)
    {
        item.Parent = _items.Parent;
        return item;
    }

    private sealed class InternalList(ObjectElement parent, 
        Action<ObjectElement, int, TObject>? adding, 
        Action<ObjectElement, TObject>? added, 
        Action<ObjectElement, TObject>? removing, 
        Action<ObjectElement, int, TObject>? removed,
        Action<ObjectElement, int, TObject, TObject>? updating,
        Action<ObjectElement, int, TObject, TObject>? updated
        ) : List<TObject>
    {
        private readonly Action<ObjectElement, int, TObject>? _adding = adding;
        private readonly Action<ObjectElement, TObject>? _added = added;
        private readonly Action<ObjectElement, TObject>? _removing = removing;
        private readonly Action<ObjectElement, int, TObject>? _removed = removed;
        private readonly Action<ObjectElement, int, TObject, TObject>? _updating = updating;
        private readonly Action<ObjectElement, int, TObject, TObject>? _updated = updated;

        public readonly ObjectElement Parent = parent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Adding(int index, TObject item) => _adding?.Invoke(Parent, index, item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Added(TObject item) => _added?.Invoke(Parent, item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removing(TObject item) => _removing?.Invoke(Parent, item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Updating(int index, TObject previousItem, TObject newItem) => _updating?.Invoke(Parent, index, previousItem, newItem);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Updated(int index, TObject previousItem, TObject newItem) => _updated?.Invoke(Parent, index, previousItem, newItem);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removed(int index, TObject removedItem) => _removed?.Invoke(Parent, index, removedItem);
    }

    internal sealed class ObjectListDebuggerView
    {
        private readonly List<TObject> _collection;

        public ObjectListDebuggerView(ObjectList<TObject> collection)
        {
            ArgumentNullException.ThrowIfNull(collection, nameof(collection));
            _collection = collection.UnsafeList;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TObject[] Items
        {
            get
            {
                var array = new TObject[_collection.Count];
                _collection.CopyTo(array, 0);
                return array;
            }
        }
    }
}
