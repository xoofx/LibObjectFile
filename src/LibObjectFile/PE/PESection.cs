// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Defines a section in a Portable Executable (PE) image.
/// </summary>
public sealed class PESection : PEObject
{
    private readonly ObjectList<PESectionData> _content;
    private PESectionVirtualSizeMode _virtualSizeMode;

    public PESection(PESectionName name, RVA rva)
    {
        Name = name;
        RVA = rva;
        _content = PEObject.CreateObjectList<PESectionData>(this);
        // Most of the time readable
        Characteristics = SectionCharacteristics.MemRead;

        // A PESection always has a custom virtual size calculated from its content
        SetCustomVirtualSize(0);
    }

    /// <summary>
    /// Gets the name of this section.
    /// </summary>
    public PESectionName Name { get; }

    public override bool HasChildren => true;

    /// <summary>
    /// Flags that describe the characteristics of the section.
    /// </summary>
    public System.Reflection.PortableExecutable.SectionCharacteristics Characteristics { get; set; }
    
    /// <summary>
    /// Gets the list of data associated with this section.
    /// </summary>
    public ObjectList<PESectionData> Content => _content;

    /// <summary>
    /// Gets the mode of the virtual size of this section.
    /// </summary>
    public PESectionVirtualSizeMode VirtualSizeMode => _virtualSizeMode;

    /// <summary>
    /// Gets or sets the stream added to the end of the section to pad it to a specific size and fill it with specific padding data.
    /// </summary>
    public Stream? PaddingStream { get; set; }

    /// <summary>
    /// Sets the virtual size of this section to be automatically computed from its raw size.
    /// </summary>
    /// <remarks>
    /// The layout of the PEFile should be updated after calling this method via <see cref="PEFile.UpdateLayout"/>.
    /// </remarks>
    public void SetVirtualSizeModeToAuto() => SetVirtualSizeMode(PESectionVirtualSizeMode.Auto, 0);

    /// <summary>
    /// Sets the virtual size of this section to a fixed size.
    /// </summary>
    /// <param name="virtualSize">The virtual size of the section.</param>
    /// <remarks>
    /// The layout of the PEFile should be updated after calling this method via <see cref="PEFile.UpdateLayout"/>.
    /// </remarks>
    public void SetVirtualSizeModeToFixed(uint virtualSize) => SetVirtualSizeMode(PESectionVirtualSizeMode.Fixed, virtualSize);

    internal void SetVirtualSizeMode(PESectionVirtualSizeMode mode, uint initialSize)
    {
        _virtualSizeMode = mode;
        SetCustomVirtualSize(initialSize);
    }
    
    /// <inheritdoc />
    public override uint GetRequiredPositionAlignment(PEFile file) => file.OptionalHeader.FileAlignment;

    /// <inheritdoc />
    public override uint GetRequiredSizeAlignment(PEFile file) => file.OptionalHeader.FileAlignment;

    /// <summary>
    /// Tries to find the section data that contains the specified virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address to search for.</param>
    /// <param name="sectionData">The section data that contains the virtual address, if found.</param>
    /// <returns><c>true</c> if the section data was found; otherwise, <c>false</c>.</returns>
    public bool TryFindSectionDataByRVA(RVA virtualAddress, [NotNullWhen(true)] out PESectionData? sectionData)
    {
        var result = _content.TryFindByRVA(virtualAddress, true, out var sectionObj);
        sectionData = sectionObj as PESectionData;
        return result && sectionData is not null;
    }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(PELayoutContext context)
    {
        context.CurrentSection = this;
        try
        {
            var peFile = context.File;

            var va = RVA;
            var position = (uint)Position;
            var size = 0U;
            foreach (var data in Content)
            {
                // Make sure we align the position and the virtual address
                var alignment = data.GetRequiredPositionAlignment(context.File);

                if (alignment > 1)
                {
                    var newPosition = AlignHelper.AlignUp(position, alignment);
                    size += newPosition - position;
                    position = newPosition;
                    va = AlignHelper.AlignUp(va, alignment);
                }

                data.RVA = va;

                if (!context.UpdateSizeOnly)
                {
                    data.Position = position;
                }

                data.UpdateLayout(context);

                var dataSize = AlignHelper.AlignUp((uint)data.Size, data.GetRequiredSizeAlignment(peFile));
                va += dataSize;
                position += dataSize;
                size += dataSize;
            }

            if (_virtualSizeMode == PESectionVirtualSizeMode.Auto)
            {
                SetCustomVirtualSize(size);
            }

            if ((Characteristics & SectionCharacteristics.ContainsUninitializedData) == 0)
            {
                // The size of a section is the size of the content aligned on the file alignment
                var fileAlignment = peFile.OptionalHeader.FileAlignment;
                var sizeWithAlignment = AlignHelper.AlignUp(size, fileAlignment);
                Size = sizeWithAlignment;
            }
            else
            {
                Size = 0;
            }
        }
        finally
        {
            context.CurrentSection = null;
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
    
    protected override bool TryFindByRVAInChildren(RVA rva, [NotNullWhen(true)] out PEObject? result) 
        => _content.TryFindByRVA(rva, true, out result);

    protected override bool TryFindByPositionInChildren(uint position, [NotNullWhen(true)] out PEObjectBase? result)
        => _content.TryFindByPosition(position, true, out result);

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