// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile;

/// <summary>
/// Helper struct to hold an element of type <see cref="ObjectFileElement"/> and ensure that it is properly set with its parent.
/// </summary>
/// <typeparam name="TObject">The type of the object to hold</typeparam>
public struct ObjectFileElementHolder<TObject> 
    where TObject : ObjectFileElement
{
    private TObject _element;

    public ObjectFileElementHolder(ObjectFileElement parent, TObject element)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(element);
        _element = null!; // Avoid warning
        Set(parent, element);
    }

    public TObject Element => _element;

    public static implicit operator TObject(ObjectFileElementHolder<TObject> holder) => holder._element;

    public void Set(ObjectFileElement parent, TObject? element)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (element?.Parent != null) throw new InvalidOperationException($"Cannot set the {element.GetType()} as it already belongs to another {element.Parent.GetType()} instance");
        
        if (_element is not null)
        {
            _element.Parent = null;
        }

        _element = element!;

        if (element != null)
        {
            element.Parent = parent;
        }
    }
}
