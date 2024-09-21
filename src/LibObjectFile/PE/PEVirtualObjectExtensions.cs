// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public static class PEVirtualObjectExtensions
{
    /// <summary>
    /// Tries to find a virtual object by its virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="item">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public static bool TryFindByVirtualAddress<TVirtualObject>(this ObjectList<TVirtualObject> list, RVA virtualAddress, uint size, [NotNullWhen(true)] out TVirtualObject? item)
        where TVirtualObject : PEVirtualObject
    {
        // Binary search
        nint low = 0;

        var dataParts = CollectionsMarshal.AsSpan(list.UnsafeList);
        nint high = dataParts.Length - 1;
        ref var firstData = ref MemoryMarshal.GetReference(dataParts);

        while (low <= high)
        {
            nint mid = low + ((high - low) >>> 1);
            var trySectionData = Unsafe.Add(ref firstData, mid);

            if (trySectionData.ContainsVirtual(virtualAddress, size))
            {
                item = trySectionData;
                return true;
            }

            if (virtualAddress > trySectionData.VirtualAddress)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        item = null;
        return false;
    }


    /// <summary>
    /// Tries to find a virtual object by its virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="item">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public static bool TryFindByVirtualAddress<TVirtualObject>(this ObjectList<TVirtualObject> list, RVA virtualAddress, bool recurse, [NotNullWhen(true)] out PEVirtualObject? item)
        where TVirtualObject : PEVirtualObject
    {
        // Binary search
        nint low = 0;

        var dataParts = CollectionsMarshal.AsSpan(list.UnsafeList);
        nint high = dataParts.Length - 1;
        ref var firstData = ref MemoryMarshal.GetReference(dataParts);

        while (low <= high)
        {
            nint mid = low + ((high - low) >>> 1);
            var trySectionData = Unsafe.Add(ref firstData, mid);

            if (trySectionData.ContainsVirtual(virtualAddress))
            {
                if (recurse && trySectionData.TryFindByVirtualAddress(virtualAddress, out var virtualItem))
                {
                    item = virtualItem;
                    return item is not null;
                }

                item = trySectionData;
                return true;
            }

            if (virtualAddress > trySectionData.VirtualAddress)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        item = null;
        return false;
    }

}