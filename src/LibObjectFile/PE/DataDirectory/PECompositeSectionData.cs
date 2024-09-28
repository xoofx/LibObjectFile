// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using LibObjectFile.Collections;
using LibObjectFile.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibObjectFile.PE;

/// <summary>
/// A section data that contains a list of <see cref="PESectionData"/> and an optional header of data.
/// </summary>
public abstract class PECompositeSectionData : PESectionData
{
    protected PECompositeSectionData()
    {
        Content = CreateObjectList<PESectionData>(this);
    }

    public sealed override bool HasChildren => true;

    internal uint HeaderSize { get; private protected set; }

    /// <summary>
    /// Gets the content of this directory.
    /// </summary>
    public ObjectList<PESectionData> Content { get; }

    public sealed override void UpdateLayout(PELayoutContext context)
    {
        var va = RVA;

        // We compute the size of the directory header
        // Each directory have a specific layout, so we delegate the computation to the derived class
        var headerSize = ComputeHeaderSize(context);
        HeaderSize = headerSize;
        va += headerSize;
        ulong size = headerSize;

        // A directory could have a content in addition to the header
        // So we update the VirtualAddress of each content and update the layout
        var position = Position + headerSize;
        foreach (var data in Content)
        {
            // Make sure we align the position and the virtual address
            var alignment = data.GetRequiredPositionAlignment(context.File);

            if (alignment > 1)
            {
                var newPosition = AlignHelper.AlignUp(position, alignment);
                size += (uint)newPosition - position;
                position = newPosition;
                va = AlignHelper.AlignUp(va, alignment);
            }

            data.RVA = va;

            // Update layout will update virtual address
            if (!context.UpdateSizeOnly)
            {
                data.Position = position;
            }
            data.UpdateLayout(context);

            var dataSize = AlignHelper.AlignUp((uint)data.Size, data.GetRequiredSizeAlignment(context.File));
            va += (uint)dataSize;
            size += dataSize;
            position += dataSize;
        }

        Size = size;
    }

    internal virtual IEnumerable<PEObjectBase> CollectImplicitSectionDataList() => Enumerable.Empty<PEObjectBase>();

    internal virtual void Bind(PEImageReader reader)
    {
    }

    internal void WriteHeaderAndContent(PEImageWriter writer)
    {
        Write(writer);

        foreach (var table in Content)
        {
            table.Write(writer);
        }
    }
    
    protected abstract uint ComputeHeaderSize(PELayoutContext context);

    protected sealed override bool TryFindByRVAInChildren(RVA rva, out PEObject? result)
        => Content.TryFindByRVA(rva, true, out result);

    protected sealed override void UpdateRVAInChildren()
    {
        var va = RVA;
        foreach (var table in Content)
        {
            table.UpdateRVA(va);
            va += (uint)table.Size;
        }
    }
}