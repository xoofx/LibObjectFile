// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LibObjectFile.PE;

/// <summary>
/// Represents an abstract base class for a PE resource entry.
/// </summary>
public abstract class PEResourceEntry : PESectionData
{
    public sealed override bool HasChildren => false;
    
    internal abstract void Read(in ReaderContext context);

    internal readonly record struct ReaderContext(PEImageReader Reader, PEResourceDirectory Directory, List<PEResourceString> Strings, List<PEResourceEntry> Entries)
    {
        private readonly Dictionary<uint, PEResourceData> _resourceDataByPosition = new();

        public bool TryFindResourceDataByPosition(uint position, [NotNullWhen(true)] out PEResourceData? resourceData)
        {
            return _resourceDataByPosition.TryGetValue(position, out resourceData);
        }

        public void AddResourceDataByPosition(uint position, PEResourceData resourceData)
        {
            _resourceDataByPosition.Add(position, resourceData);
        }


        public bool TryFindResourceStringByPosition(uint position, [NotNullWhen(true)] out PEResourceString? resourceString)
        {
            resourceString = null;

            foreach (var item in Strings)
            {
                if (item.Position == position)
                {
                    resourceString = item;
                    return true;
                }
            }
            return false;
        }

    }

    /// <inheritdoc/>
    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PEResourceDirectory)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PEResourceDirectory).FullName}");
        }
    }
}
