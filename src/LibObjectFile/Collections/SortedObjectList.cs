// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LibObjectFile.Collections;

/// <summary>
/// A list of objects that are attached to a parent object.
/// </summary>
/// <typeparam name="TObject">The type of the object file.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(SortedObjectList<>.ObjectListDebuggerView))]
public readonly struct SortedObjectList<TObject> : IList<TObject>
    where TObject : ObjectElement, IComparable<TObject>
{
    // We are using an internal list to keep track of the parent object
    private readonly InternalList _items;

    [Obsolete("This constructor is not supported", true)]
    public SortedObjectList() => throw new NotSupportedException("This constructor is not supported");

    /// <summary>
    /// Initializes a new instance of the <see cref="SortedObjectList{TObject}"/> class.
    /// </summary>
    /// <param name="parent">The parent object file node.</param>
    public SortedObjectList(ObjectElement parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        _items = new InternalList(parent);
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public List<TObject> UnsafeList => _items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TObject item)
    {
        CheckAdd(item);
        var items = _items;
        if (items.Count > 0 && item.CompareTo(items[^1]) > 0)
        {
            int index = items.Count;
            items.Add(AssignAdd(item));
            item.Index = index;
        }
        else
        {
            var index = items.BinarySearch(item);
            if (index < 0)
            {
                index = ~index;
            }
            items.Insert(index, AssignAdd(item));
            item.Index = index;
        }
    }

    public void Clear()
    {
        var items = _items;
        for (var i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            items.RemoveAt(i);
            item.Parent = null;
            item.ResetIndex();
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

        var expectedIndex = items.BinarySearch(item);
        if (expectedIndex < 0)
        {
            expectedIndex = ~expectedIndex;
        }

        if (expectedIndex != index)
        {
            throw new ArgumentException($"The index {index} is not valid for the item {item} to maintain the order of the list");
        }

        items.Insert(index, AssignAdd(item));

        for (int i = index; i < items.Count; i++)
        {
            items[i].Index = i;
        }
    }

    public void RemoveAt(int index)
    {
        var items = _items;
        var item = items[index];
        item.Parent = null;
        item.ResetIndex();

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
            if ((uint)index >= (uint)_items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            CheckAdd(value);

            // Unbind previous entry
            var items = _items;

            var expectedIndex = items.BinarySearch(value);
            if (expectedIndex < 0)
            {
                expectedIndex = ~expectedIndex;
            }
            if (expectedIndex != index)
            {
                throw new ArgumentException($"The index {index} is not valid for the item {value} to maintain the order of the list");
            }

            if (index < items.Count)
            {
                var previousItem = items[index];
                previousItem.Parent = null;
                previousItem.ResetIndex();

                // Bind new entry
                items[index] = AssignAdd(value);
                value.Index = index;
            }
            else
            {
                items.Add(AssignAdd(value));
                value.Index = items.Count - 1;
            }
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

    private sealed class InternalList(ObjectElement parent) : List<TObject>
    {
        public readonly ObjectElement Parent = parent;
    }
    
    internal sealed class ObjectListDebuggerView
    {
        private readonly List<TObject> _collection;

        public ObjectListDebuggerView(SortedObjectList<TObject> collection)
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
