// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a section in a Portable Executable (PE) image.
/// </summary>
public sealed class PESection : PEObject
{
    private readonly ObjectList<PESectionData> _content;

    public PESection(PESectionName name, RVA rva, RVA virtualSize)
    {
        Name = name;
        RVA = rva;
        VirtualSize = virtualSize;
        _content = PEObject.CreateObjectList<PESectionData>(this);
        // Most of the time readable
        Characteristics = SectionCharacteristics.MemRead;
    }

    /// <summary>
    /// Gets the name of this section.
    /// </summary>
    public PESectionName Name { get; }

    public override bool HasChildren => true;

    /// <summary>
    /// The total size of the section when loaded into memory.
    /// If this value is greater than <see cref="Size"/>, the section is zero-padded.
    /// </summary>
    public override uint VirtualSize { get; }
    
    /// <summary>
    /// Flags that describe the characteristics of the section.
    /// </summary>
    public System.Reflection.PortableExecutable.SectionCharacteristics Characteristics { get; set; }
    
    /// <summary>
    /// Gets the list of data associated with this section.
    /// </summary>
    public ObjectList<PESectionData> Content => _content;

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="sectionData">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public bool TryFindSectionData(RVA virtualAddress, [NotNullWhen(true)] out PESectionData? sectionData)
    {
        var result = _content.TryFindByRVA(virtualAddress, true, out var sectionObj);
        sectionData = sectionObj as PESectionData;
        return result && sectionData is not null;
    }

    /// <inheritdoc />
    public override void UpdateLayout(PELayoutContext context)
    {
        var va = RVA;
        var position = Position;
        foreach (var data in Content)
        {
            data.RVA = va;
            if (!context.UpdateSizeOnly)
            {
                data.Position = position;
            }

            data.UpdateLayout(context);
            va += (uint)data.Size;
            position += data.Size;
        }
    }

    public override void Read(PEImageReader reader)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{Name} ");
        base.PrintMembers(builder);
        builder.Append($", Content[{Content.Count}]");
        return true;
    }
    
    protected override bool TryFindByRVAInChildren(RVA rva, out PEObject? result) 
        => _content.TryFindByRVA(rva, true, out result);

    protected override void UpdateRVAInChildren()
    {
        // TODO?
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PEFile)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PEFile).FullName}");
        }
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
}