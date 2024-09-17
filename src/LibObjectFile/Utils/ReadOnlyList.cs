// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Utils;

/// <summary>
/// A lightweight read-only wrapper around a List&lt;T> that avoids the cost of interface dispatch from IReadOnlyList&lt;T>.
/// </summary>
/// <typeparam name="T">The type of the collection</typeparam>
[DebuggerTypeProxy(typeof(ReadOnlyList<>.ReadOnlyListView))]
[DebuggerDisplay("Count = {Count}")]
public readonly struct ReadOnlyList<T> : IReadOnlyList<T>
{
    private readonly List<T> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyList{T}"/> class.
    /// </summary>
    /// <param name="items">The items to wrap.</param>
    public ReadOnlyList(List<T> items) => _items = items;

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public T this[int index] => _items[index];

    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }

    /// <summary>
    /// Converts a List&lt;T> to a ReadOnlyList&lt;T>.
    /// </summary>
    /// <param name="items">The list to convert.</param>
    public static implicit operator ReadOnlyList<T>(List<T> items) => new(items);
    
    internal sealed class ReadOnlyListView
    {
        private readonly List<T> _collection;

        public ReadOnlyListView(List<T> collection)
        {
            ArgumentNullException.ThrowIfNull((object)collection, nameof(collection));
            this._collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] array = new T[this._collection.Count];
                this._collection.CopyTo(array, 0);
                return array;
            }
        }
    }
}