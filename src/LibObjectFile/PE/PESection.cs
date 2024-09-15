// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a section in a Portable Executable (PE) image.
/// </summary>
public class PESection : PEObject
{
    private readonly List<PESectionData> _dataList;

    internal PESection(PEFile peFile, PESectionName name)
    {
        Parent = peFile;
        Name = name;
        _dataList = new List<PESectionData>();
        // Most of the time readable
        Characteristics = SectionCharacteristics.MemRead;
    }

    /// <summary>
    /// Gets the parent <see cref="PEFile"/> of this section.
    /// </summary>
    public PEFile? ImageFile => (PEFile?)Parent;

    /// <summary>
    /// Gets the name of this section.
    /// </summary>
    public PESectionName Name { get; }

    /// <summary>
    /// The address of the first byte of the section when loaded into memory, relative to the image base.
    /// </summary>
    public uint VirtualAddress { get; set; }

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
    public IReadOnlyList<PESectionData> DataList => _dataList;

    /// <summary>
    /// Adds a new data to this section.
    /// </summary>
    /// <param name="data">The data to add.</param>
    /// <exception cref="ArgumentException">If the data is already associated with a section.</exception>
    public void AddData(PESectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Section != null) throw new ArgumentException("Data is already associated with a section", nameof(data));
        data.Section = this;
        _dataList.Add(data);
    }

    /// <summary>
    /// Removes the specified data from this section.
    /// </summary>
    /// <param name="data">The data to remove.</param>
    /// <exception cref="ArgumentException">If the data is already associated with a section.</exception>
    public void RemoveData(PESectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Section != this) throw new ArgumentException("Data is not associated with this section", nameof(data));
        data.Section = null;
        _dataList.Remove(data);
    }

    /// <summary>
    /// Removes the data at the specified index.
    /// </summary>
    /// <param name="index">The index of the data to remove.</param>
    public void RemoveDataAt(int index)
    {
        var data = _dataList[index];
        data.Section = null;
        _dataList.RemoveAt(index);
    }

    /// <summary>
    /// Checks if the specified virtual address is contained by this section.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this section.</param>
    /// <returns><c>true</c> if the virtual address is within the section range.</returns>
    public bool ContainsVirtual(uint virtualAddress)
    {
        return virtualAddress >= VirtualAddress && virtualAddress < VirtualAddress + VirtualSize;
    }

    /// <summary>
    /// Checks if the specified virtual address and size is contained by this section.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to check if it belongs to this section.</param>
    /// <param name="size">The size to check if it belongs to this section.</param>
    /// <returns><c>true</c> if the virtual address and size is within the section range.</returns>
    public bool ContainsVirtual(uint virtualAddress, uint size)
    {
        return virtualAddress >= VirtualAddress && virtualAddress + size <= VirtualAddress + VirtualSize;
    }
    
    /// <inheritdoc />
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        foreach (var data in DataList)
        {
            data.UpdateLayout(diagnostics);
        }
    }
}