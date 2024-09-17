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
    private readonly List<PESectionData> _dataParts;
    private RVA _virtualAddress;

    internal PESection(PEFile peFile, PESectionName name)
    {
        base.Parent = peFile;
        Name = name;
        _dataParts = new List<PESectionData>();
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
    public RVA VirtualAddress
    {
        get => _virtualAddress;
        set
        {
            _virtualAddress = value;
            UpdateSectionDataIndicesAndVirtualAddress(0);
        }
    }

    /// <summary>
    /// The total size of the section when loaded into memory.
    /// If this value is greater than <see cref="Size"/>, the section is zero-padded.
    /// </summary>
    public uint VirtualSize { get; set; }
    
    /// <summary>
    /// The file pointer to the beginning of the relocation entries for the section, if present.
    /// </summary>
    public uint PointerToRelocations { get; set; }

    /// <summary>
    /// The file pointer to the beginning of the line-number entries for the section, if present.
    /// </summary>
    public uint PointerToLineNumbers { get; set; }

    /// <summary>
    /// The number of relocation entries for the section.
    /// </summary>
    public ushort NumberOfRelocations { get; set; }

    /// <summary>
    /// The number of line-number entries for the section.
    /// </summary>
    public ushort NumberOfLineNumbers { get; set; }

    /// <summary>
    /// Flags that describe the characteristics of the section.
    /// </summary>
    public System.Reflection.PortableExecutable.SectionCharacteristics Characteristics { get; set; }
    
    /// <summary>
    /// Gets the list of data associated with this section.
    /// </summary>
    public ReadOnlyList<PESectionData> DataParts => _dataParts;
    
    /// <summary>
    /// Adds a new data to this section.
    /// </summary>
    /// <param name="data">The data to add.</param>
    /// <exception cref="ArgumentException">If the data is already associated with a section.</exception>
    public void AddData(PESectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Parent != null) throw new ArgumentException("Data is already associated with a section", nameof(data));
        data.Parent = this;
        data.Index = (uint)_dataParts.Count;
        _dataParts.Add(data);
        UpdateSectionDataIndicesAndVirtualAddress((int)data.Index);
    }

    public void InsertData(int index, PESectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (index < 0 || index > _dataParts.Count) throw new ArgumentOutOfRangeException(nameof(index));

        if (data.Parent != null) throw new ArgumentException("Data is already associated with a section", nameof(data));
        data.Parent = this;
        data.Index = (uint)index;
        _dataParts.Insert(index, data);
        UpdateSectionDataIndicesAndVirtualAddress(index);
    }

    /// <summary>
    /// Removes the specified data from this section.
    /// </summary>
    /// <param name="data">The data to remove.</param>
    /// <exception cref="ArgumentException">If the data is already associated with a section.</exception>
    public void RemoveData(PESectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Parent != this) throw new ArgumentException("Data is not associated with this section", nameof(data));
        var index = (int)data.Index;
        RemoveDataAt(index);
    }

    /// <summary>
    /// Removes the data at the specified index.
    /// </summary>
    /// <param name="index">The index of the data to remove.</param>
    public void RemoveDataAt(int index)
    {
        if (index < 0 || index > _dataParts.Count) throw new ArgumentOutOfRangeException(nameof(index));

        var list = _dataParts;
        var data = list[index];
        data.Parent = null;
        data.Index = 0;
        list.RemoveAt(index);
        UpdateSectionDataIndicesAndVirtualAddress(index);
    }

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="sectionData">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public bool TryFindSectionData(RVA virtualAddress, [NotNullWhen(true)] out PESectionData? sectionData)
    {
        // Binary search
        nint left = 0;

        var dataParts = CollectionsMarshal.AsSpan(_dataParts);
        nint right = dataParts.Length - 1;
        ref var firstData = ref MemoryMarshal.GetReference(dataParts);

        while (left <= right)
        {
            nint mid = left + (right - left) >>> 1;
            var trySectionData = Unsafe.Add(ref firstData, mid);

            if (trySectionData.ContainsVirtual(virtualAddress))
            {
                sectionData = trySectionData;
                return true;
            }

            if (trySectionData.VirtualAddress < virtualAddress)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
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
    public bool ContainsVirtual(RVA virtualAddress)
    {
        return virtualAddress >= VirtualAddress && virtualAddress < VirtualAddress + VirtualSize;
    }

    /// <summary>
    /// Checks if the specified virtual address and size is contained by this section.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this section.</param>
    /// <param name="size">The size to check if it belongs to this section.</param>
    /// <returns><c>true</c> if the virtual address and size is within the section range.</returns>
    public bool ContainsVirtual(RVA virtualAddress, uint size)
    {
        return virtualAddress >= VirtualAddress && virtualAddress + size <= VirtualAddress + VirtualSize;
    }
    
    /// <inheritdoc />
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        foreach (var data in DataParts)
        {
            data.UpdateLayout(diagnostics);
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

    private void UpdateSectionDataIndicesAndVirtualAddress(int startIndex)
    {
        var va = VirtualAddress;
        var list = _dataParts;
        if (startIndex > 0)
        {
            var previousData = list[startIndex - 1];
            va = previousData.VirtualAddress + (uint)previousData.Size;
        }
        
        for (int i = startIndex; i < list.Count; i++)
        {
            var data = list[i];
            data.Index = (uint)i;
            data.VirtualAddress = va;
            va += (uint)data.Size;
        }
    }
}