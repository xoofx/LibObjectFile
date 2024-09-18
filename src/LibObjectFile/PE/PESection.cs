// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a section in a Portable Executable (PE) image.
/// </summary>
public class PESection : PEObject, IVirtualAddressable
{
    private readonly ObjectList<PESectionData> _dataParts;

    public PESection(PESectionName name, RVA virtualAddress, RVA virtualSize)
    {
        Name = name;
        VirtualAddress = virtualAddress;
        VirtualSize = virtualSize;
        _dataParts = new ObjectList<PESectionData>(this, SectionDataAdded, null, SectionDataRemoved, SectionDataUpdated);
        // Most of the time readable
        Characteristics = SectionCharacteristics.MemRead;
    }

    /// <summary>
    /// Gets the parent <see cref="PEFile"/> of this section.
    /// </summary>
    public new PEFile? Parent => (PEFile?)base.Parent;

    /// <summary>
    /// Gets the name of this section.
    /// </summary>
    public PESectionName Name { get; }

    /// <summary>
    /// The address of the first byte of the section when loaded into memory, relative to the image base.
    /// </summary>
    public RVA VirtualAddress { get; }

    /// <summary>
    /// The total size of the section when loaded into memory.
    /// If this value is greater than <see cref="Size"/>, the section is zero-padded.
    /// </summary>
    public uint VirtualSize { get; }
    
    /// <summary>
    /// Flags that describe the characteristics of the section.
    /// </summary>
    public System.Reflection.PortableExecutable.SectionCharacteristics Characteristics { get; set; }
    
    /// <summary>
    /// Gets the list of data associated with this section.
    /// </summary>
    public ObjectList<PESectionData> DataParts => _dataParts;

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="sectionData">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public bool TryFindSectionData(RVA virtualAddress, [NotNullWhen(true)] out PESectionData? sectionData)
    {
        // Binary search
        nint low = 0;

        var dataParts = CollectionsMarshal.AsSpan(_dataParts.UnsafeList);
        nint high = dataParts.Length - 1;
        ref var firstData = ref MemoryMarshal.GetReference(dataParts);

        while (low <= high)
        {
            nint mid = low + (high - low) >>> 1;
            var trySectionData = Unsafe.Add(ref firstData, mid);

            if (trySectionData.ContainsVirtual(virtualAddress))
            {
                sectionData = trySectionData;
                return true;
            }

            if (trySectionData.VirtualAddress < virtualAddress)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        sectionData = null;
        return false;
    }

    /// <summary>
    /// Checks if the specified virtual address is contained by this section.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this section.</param>
    /// <returns><c>true</c> if the virtual address is within the section range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVirtual(RVA virtualAddress) 
        => virtualAddress >= VirtualAddress && virtualAddress < VirtualAddress + VirtualSize;

    /// <summary>
    /// Checks if the specified virtual address and size is contained by this section.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this section.</param>
    /// <param name="size">The size to check if it belongs to this section.</param>
    /// <returns><c>true</c> if the virtual address and size is within the section range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVirtual(RVA virtualAddress, uint size) 
        => virtualAddress >= VirtualAddress && virtualAddress + size <= VirtualAddress + VirtualSize;

    /// <inheritdoc />
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        var va = VirtualAddress;
        foreach (var data in DataParts)
        {
            data.VirtualAddress = va;
            data.UpdateLayout(diagnostics);
            va += (uint)data.Size;
        }
    }

    protected override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override void PrintName(StringBuilder builder)
    {
        builder.Append(Name);
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"VirtualAddress = {VirtualAddress}, VirtualSize = 0x{VirtualSize:X4}, DataParts[{DataParts.Count}]");
        return true;
    }

    /// <summary>
    /// Gets the default characteristics for a section name.
    /// </summary>
    /// <param name="sectionName">The name of the section</param>
    /// <returns>The default characteristics for the specified section name.</returns>
    /// <remarks>
    /// The default characteristics are:
    /// <list type="bullet">
    ///  <item><description>.text: ContainsCode | MemExecute | MemRead</description></item>
    ///  <item><description>.data: ContainsInitializedData | MemRead | MemWrite</description></item>
    ///  <item><description>.bss: ContainsUninitializedData | MemRead | MemWrite</description></item>
    ///  <item><description>.idata: ContainsInitializedData | MemRead | MemWrite</description></item>
    ///  <item><description>.reloc: ContainsInitializedData | MemDiscardable | MemRead</description></item>
    ///  <item><description>.tls: ContainsInitializedData | MemRead | MemWrite</description></item>
    /// </list>
    ///
    /// Otherwise the default characteristics is ContainsInitializedData | MemRead.
    /// </remarks>
    public static SectionCharacteristics GetDefaultSectionCharacteristics(PESectionName sectionName)
    {
        return sectionName.Name switch
        {
            ".text" => SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead,
            ".data" => SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite,
            ".bss" => SectionCharacteristics.ContainsUninitializedData | SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite,
            ".idata" => SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite,
            ".reloc" => SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemDiscardable | SectionCharacteristics.MemRead,
            ".tls" => SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite,
            _ => SectionCharacteristics.ContainsInitializedData |  SectionCharacteristics.MemRead
        };
    }
    
    private static void SectionDataAdded(ObjectFileNode parent, PESectionData sectionData)
    {
        var section = (PESection) parent;
        section.UpdateSectionDataVirtualAddress(sectionData.Index);
    }
    
    private static void SectionDataRemoved(ObjectFileNode parent, int index, PESectionData removedSectionData)
    {
        var section = (PESection) parent;
        section.UpdateSectionDataVirtualAddress(index);
    }
    private static void SectionDataUpdated(ObjectFileNode parent, int index, PESectionData previousSectionData, PESectionData newSectionData)
    {
        var section = (PESection) parent;
        section.UpdateSectionDataVirtualAddress(index);
    }

    private void UpdateSectionDataVirtualAddress(int startIndex)
    {
        var va = VirtualAddress;
        var list = _dataParts.UnsafeList;
        if (startIndex > 0)
        {
            var previousData = list[startIndex - 1];
            va = previousData.VirtualAddress + (uint)previousData.Size;
        }
        
        for (int i = startIndex; i < list.Count; i++)
        {
            var data = list[i];
            data.VirtualAddress = va;
            va += (uint)data.Size;
        }
    }
}