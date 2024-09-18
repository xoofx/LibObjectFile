// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils;

/// <summary>
/// A list of objects that are attached to a parent object.
/// </summary>
/// <typeparam name="TObject">The type of the object file.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ObjectList<>.ObjectListDebuggerView))]
public readonly struct ObjectList<TObject> : IList<TObject>
    where TObject : ObjectFileElement
{
    // We are using an internal list to keep track of the parent object
    private readonly InternalList _items;

    [Obsolete("This constructor is not supported", true)]
    public ObjectList() => throw new NotSupportedException("This constructor is not supported");

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectList{TObject}"/> class.
    /// </summary>
    /// <param name="parent">The parent object file node.</param>
    public ObjectList(ObjectFileElement parent, Action<ObjectFileElement, TObject>? added = null, Action<ObjectFileElement, TObject>? removing = null, Action<ObjectFileElement, int, TObject>? removed = null,  Action<ObjectFileElement, int, TObject, TObject>? updated = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        _items = new InternalList(parent, added, removing, removed, updated);
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public List<TObject> UnsafeList => _items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TObject item)
    {
        var items = _items;
        int index = items.Count;
        items.Add(CheckAdd(item));
        item.Index = index;
        items.Added(item);
    }

    public void Clear()
    {
        var items = _items;
        for (var i = items.Count - 1; i >= 0; i++)
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
        var items = _items;
        items.Insert(index, CheckAdd(item));
        
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
        item.Parent = null;
        item.Index = 0;
        
        items.RemoveAt(index);

        for (int i = index; i < items.Count; i++)
        {
            items[i].Index = i;
        }
    }

    public TObject this[int index]
    {
        get => _items[index];
        set
        {
            value = CheckAdd(value);

            // Unbind previous entry
            var items = _items;
            var previousItem = items[index];
            items.Removing(previousItem);
            previousItem.Parent = null;
            previousItem.ResetIndex();

            // Bind new entry
            items[index] = value;
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
    
    public TObject CheckAdd(TObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Parent != null)
        {
            throw new ArgumentException($"The object is already attached to another parent", nameof(item));
        }
        item.Parent = _items.Parent;
        return item;
    }
    
    private sealed class InternalList(ObjectFileElement parent, Action<ObjectFileElement, TObject>? added, Action<ObjectFileElement, TObject>? removing, Action<ObjectFileElement, int, TObject>? removed, Action<ObjectFileElement, int, TObject, TObject>? updated) : List<TObject>
    {
        private readonly Action<ObjectFileElement, TObject>? _added = added;
        private readonly Action<ObjectFileElement, TObject>? _removing = removing;
        private readonly Action<ObjectFileElement, int, TObject>? _removed = removed;
        private readonly Action<ObjectFileElement, int, TObject, TObject>? _updated = updated;
        public readonly ObjectFileElement Parent = parent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Added(TObject item) => _added?.Invoke(Parent, item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removing(TObject item) => _removing?.Invoke(Parent, item);
        
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
            ArgumentNullException.ThrowIfNull((object)collection, nameof(collection));
            this._collection = collection.UnsafeList;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TObject[] Items
        {
            get
            {
                var array = new TObject[this._collection.Count];
                _collection.CopyTo(array, 0);
                return array;
            }
        }
    }
}
