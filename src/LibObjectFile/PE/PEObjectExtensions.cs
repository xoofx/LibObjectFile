// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public static class PEObjectExtensions
{
    public static bool IsNull<TRVALink>(this TRVALink link) where TRVALink : IPELink<PEObjectBase> => link.Container is null;

    public static string ToDisplayText<TRVALink>(this TRVALink link) where TRVALink : IPELink<PEObjectBase> => link.Container is not null ? $"{link.Container}, Offset = {link.RVO}" : $"<empty>";
    
    public static RVA RVA<TRVALink>(this TRVALink link) where TRVALink : IPELink<PEObject> => link.Container is not null ? link.Container.RVA + link.RVO : 0;

    public static string ToDisplayTextWithRVA<TRVALink>(this TRVALink link) where TRVALink : IPELink<PEObject> => link.Container is not null ? $"RVA = {RVA(link)}, {link.Container}, Offset = {link.RVO}" : $"<empty>";


    /// <summary>
    /// Tries to find a virtual object by its virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="item">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public static bool TryFindByRVA<TPEObject>(this ObjectList<TPEObject> list, RVA virtualAddress, uint size, [NotNullWhen(true)] out TPEObject? item)
        where TPEObject : PEObject
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

            if (virtualAddress > trySectionData.RVA)
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
    public static bool TryFindByRVA<TPEObject>(this ObjectList<TPEObject> list, RVA virtualAddress, bool recurse, [NotNullWhen(true)] out PEObject? item)
        where TPEObject : PEObject
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
                if (recurse && trySectionData.TryFindByRVA(virtualAddress, out var virtualItem))
                {
                    item = virtualItem;
                    return item is not null;
                }

                item = trySectionData;
                return true;
            }

            if (virtualAddress > trySectionData.RVA)
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