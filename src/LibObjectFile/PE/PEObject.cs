// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
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
    private uint _customVirtualSize;
    private bool _useCustomVirtualSize;

    protected PEObject()
    {
    }

    /// <summary>
    /// The address of the first byte of the section when loaded into memory, relative to the image base.
    /// </summary>
    public RVA RVA { get; internal set; }

    /// <summary>
    /// The size of this object in virtual memory.
    /// </summary>
    public uint VirtualSize => _useCustomVirtualSize ? _customVirtualSize : (uint)Size;

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
    public bool TryFindByRVA(RVA rva, [NotNullWhen(true)] out PEObject? result)
    {
        if (ContainsVirtual(rva))
        {
            if (HasChildren && TryFindByRVAInChildren(rva, out result))
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
    protected virtual bool TryFindByRVAInChildren(RVA rva, [NotNullWhen(true)] out PEObject? result)
    {
        throw new NotImplementedException("This method must be implemented by PEVirtualObject with children");
    }

    internal void UpdateRVA(RVA rva)
    {
        RVA = rva;
        if (HasChildren)
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
        builder.Append($"RVA = {RVA}, VirtualSize = 0x{VirtualSize:X}, ");
        base.PrintMembers(builder);
        return true;
    }

    protected override void ValidateParent(ObjectElement parent)
    {
    }

    private protected void SetCustomVirtualSize(uint customVirtualSize)
    {
        _customVirtualSize = customVirtualSize;
        _useCustomVirtualSize = true;
    }

    private protected void ResetCustomVirtualSize()
    {
        _useCustomVirtualSize = false;
    }

    public sealed override void UpdateLayout(PELayoutContext layoutContext)
    {
        var section = layoutContext.CurrentSection;

        if (section is null)
        {
            // Update the layout normally
            base.UpdateLayout(layoutContext);
        }
        else
        {
            // If the section is auto, we reset the custom virtual size to Size
            if (section.VirtualSizeMode == PESectionVirtualSizeMode.Auto)
            {
                ResetCustomVirtualSize();
            }

            var currentSectionVirtualSize = this.RVA - section.RVA;
            
            // Update the layout normally
            base.UpdateLayout(layoutContext);

            // Accumulate the virtual size of the section
            currentSectionVirtualSize += (uint)Size;

            // If the section is fixed, we update the virtual size of the current object being visited
            if (section.VirtualSizeMode == PESectionVirtualSizeMode.Fixed && currentSectionVirtualSize > section.VirtualSize)
            {
                var virtualSizeToRemove = currentSectionVirtualSize - section.VirtualSize;
                if (virtualSizeToRemove >= VirtualSize)
                {
                    SetCustomVirtualSize(0);
                }
                else
                {
                    SetCustomVirtualSize(VirtualSize - virtualSizeToRemove);
                }
            }
        }
    }

    public static ObjectList<TPEObject> CreateObjectList<TPEObject>(PEObject parent) where TPEObject : PEObject
    {
        ObjectList<TPEObject> objectList = default;
        objectList = new ObjectList<TPEObject>(parent, null, SectionDataAdded, null, SectionDataRemoved, null, SectionDataUpdated);
        return objectList;

        void SectionDataAdded(ObjectElement vParent, TPEObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, item.Index);
        }

        void SectionDataRemoved(ObjectElement vParent, int index, TPEObject item)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, index);
        }

        void SectionDataUpdated(ObjectElement vParent, int index, TPEObject previousItem, TPEObject newItem)
        {
            // ReSharper disable once AccessToModifiedClosure
            UpdateSectionDataRVA((PEObject)vParent, objectList, index);
        }

        static void UpdateSectionDataRVA(PEObject parent, ObjectList<TPEObject> items, int startIndex)
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