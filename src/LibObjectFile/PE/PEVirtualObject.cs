// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for all Portable Executable (PE) objects that have a virtual address.
/// </summary>
public abstract class PEVirtualObject : PEObject
{
    protected PEVirtualObject(bool hasChildren)
    {
        HasChildren = hasChildren;
    }

    /// <summary>
    /// Gets a value indicating whether this object has children.
    /// </summary>
    public bool HasChildren { get; }

    /// <summary>
    /// The address of the first byte of the section when loaded into memory, relative to the image base.
    /// </summary>
    public RVA RVA { get; internal set; }

    /// <summary>
    /// The size of this object in virtual memory.
    /// </summary>
    public virtual uint VirtualSize => (uint)Size;

    /// <summary>
    /// Checks if the specified virtual address is contained in this object.
    /// </summary>
    /// <param name="rva">The virtual address to check.</param>
    /// <returns><c>true</c> if the specified virtual address is contained in this object; otherwise, <c>false</c>.</returns>
    public bool ContainsVirtual(RVA rva)
        => RVA <=  rva && rva < RVA + VirtualSize;

    /// <summary>
    /// Checks if the specified virtual address is contained in this object.
    /// </summary>
    /// <param name="rva">The virtual address to check.</param>
    /// <param name="size">The size of the data that must be contained.</param>
    /// <returns><c>true</c> if the specified virtual address is contained in this object; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVirtual(RVA rva, uint size) 
        => RVA <= rva && rva + size <= RVA + VirtualSize;

    /// <summary>
    /// Tries to find a virtual object by its virtual address.
    /// </summary>
    /// <param name="rva">The virtual address to search for.</param>
    /// <param name="result">The virtual object that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the virtual object was found; otherwise, <c>false</c>.</returns>
    public bool TryFindByVirtualAddress(RVA rva, out PEVirtualObject? result)
    {
        if (ContainsVirtual(rva))
        {
            if (HasChildren && TryFindByVirtualAddressInChildren(rva, out result))
            {
                return true;
            }

            result = this;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Try to find a virtual object by its virtual address in children.
    /// </summary>
    /// <param name="rva">The virtual address to search for.</param>
    /// <param name="result">The virtual object that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the virtual object was found; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool TryFindByVirtualAddressInChildren(RVA rva, out PEVirtualObject? result)
    {
        throw new NotImplementedException("This method must be implemented by PEVirtualObject with children");
    }

    internal void UpdateVirtualAddress(RVA rva)
    {
        RVA = rva;
        if (HasChildren)
        {
            UpdateVirtualAddressInChildren();
        }
    }

    /// <summary>
    /// Updates the virtual address of children.
    /// </summary>
    protected virtual void UpdateVirtualAddressInChildren()
    {
    }

    public static ObjectList<TVirtualObject> CreateObjectList<TVirtualObject>(PEVirtualObject parent) where TVirtualObject : PEVirtualObject
    {
        ObjectList<TVirtualObject> objectList = default;
        objectList = new ObjectList<TVirtualObject>(parent, null, SectionDataAdded, null, SectionDataRemoved, null, SectionDataUpdated);
        return objectList;

        void SectionDataAdded(ObjectFileElement vParent, TVirtualObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataVirtualAddress((PEVirtualObject)vParent, objectList, item.Index);
        }

        void SectionDataRemoved(ObjectFileElement vParent, int index, TVirtualObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataVirtualAddress((PEVirtualObject)vParent, objectList, index);
        }

        void SectionDataUpdated(ObjectFileElement vParent, int index, TVirtualObject previousItem, TVirtualObject newItem)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataVirtualAddress((PEVirtualObject)vParent, objectList, index);
        }

        static void UpdateSectionDataVirtualAddress(PEVirtualObject parent, ObjectList<TVirtualObject> items, int startIndex)
        {
            RVA va;
            var span = CollectionsMarshal.AsSpan(items.UnsafeList);
            if (startIndex > 0)
            {
                var previousData = span[startIndex - 1];
                va = previousData.RVA + (uint)previousData.Size;
            }
            else
            {
                va = parent.RVA;
                if (parent is PEDataDirectory directory)
                {
                    va += directory.HeaderSize;
                }
            }

            for (int i = startIndex; i < span.Length; i++)
            {
                var data = span[i];

                data.RVA = va;
                data.UpdateVirtualAddressInChildren();

                va += (uint)data.Size;
            }
        }
    }
}