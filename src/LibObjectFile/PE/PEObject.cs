﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

/// <summary>
/// Base class for all Portable Executable (PE) objects that have a virtual address.
/// </summary>
public abstract class PEObject : PEObjectBase
{
    protected PEObject(bool isContainer)
    {
        IsContainer = isContainer;
    }

    /// <summary>
    /// Gets a value indicating whether this object has children.
    /// </summary>
    public bool IsContainer { get; }

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
    public bool TryFindByRVA(RVA rva, out PEObject? result)
    {
        if (ContainsVirtual(rva))
        {
            if (IsContainer && TryFindByRVAInChildren(rva, out result))
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
    protected virtual bool TryFindByRVAInChildren(RVA rva, out PEObject? result)
    {
        throw new NotImplementedException("This method must be implemented by PEVirtualObject with children");
    }

    internal void UpdateRVA(RVA rva)
    {
        RVA = rva;
        if (IsContainer)
        {
            UpdateRVAInChildren();
        }
    }

    /// <summary>
    /// Updates the virtual address of children.
    /// </summary>
    protected virtual void UpdateRVAInChildren()
    {
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"RVA = {RVA}, VirtualSize = 0x{VirtualSize:X}, Size = 0x{Size:X}");
        return true;
    }

    protected override void ValidateParent(ObjectFileElement parent)
    {
    }

    public static ObjectList<TVirtualObject> CreateObjectList<TVirtualObject>(PEObject parent) where TVirtualObject : PEObject
    {
        ObjectList<TVirtualObject> objectList = default;
        objectList = new ObjectList<TVirtualObject>(parent, null, SectionDataAdded, null, SectionDataRemoved, null, SectionDataUpdated);
        return objectList;

        void SectionDataAdded(ObjectFileElement vParent, TVirtualObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, item.Index);
        }

        void SectionDataRemoved(ObjectFileElement vParent, int index, TVirtualObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, index);
        }

        void SectionDataUpdated(ObjectFileElement vParent, int index, TVirtualObject previousItem, TVirtualObject newItem)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, index);
        }

        static void UpdateSectionDataRVA(PEObject parent, ObjectList<TVirtualObject> items, int startIndex)
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
                data.UpdateRVAInChildren();

                va += (uint)data.Size;
            }
        }
    }
}