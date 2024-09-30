// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibObjectFile.Collections;
using LibObjectFile.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibObjectFile.PE;

/// <summary>
/// A section data that contains a list of <see cref="PESectionData"/> and an optional header of data.
/// </summary>
public abstract class PECompositeSectionData : PESectionData, IEnumerable<PESectionData>
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

    protected sealed override void UpdateLayoutCore(PELayoutContext context)
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
            var position = writer.Position;
            var alignment = table.GetRequiredPositionAlignment(writer.PEFile);
            if (alignment > 1)
            {
                var zeroSize = AlignHelper.AlignUp(position, alignment) - (uint)position;
                writer.WriteZero((int)zeroSize);
            }

            Debug.Assert(table.Position == writer.Position);

            if (table is PECompositeSectionData compositeSectionData)
            {
                compositeSectionData.WriteHeaderAndContent(writer);
            }
            else
            {
                table.Write(writer);
            }

            alignment = table.GetRequiredSizeAlignment(writer.PEFile);
            if (alignment > 1)
            {
                var zeroSize = AlignHelper.AlignUp((uint)table.Size, alignment) - table.Size;
                writer.WriteZero((int)zeroSize);
            }
        }
    }
    
    protected abstract uint ComputeHeaderSize(PELayoutContext context);

    protected sealed override bool TryFindByRVAInChildren(RVA rva, [NotNullWhen(true)] out PEObject? result)
        => Content.TryFindByRVA(rva, true, out result);

    protected sealed override bool TryFindByPositionInChildren(uint position, [NotNullWhen(true)] out PEObjectBase? result)
        => Content.TryFindByPosition(position, true, out result);

    protected sealed override void UpdateRVAInChildren()
    {
        var va = RVA;
        foreach (var table in Content)
        {
            table.UpdateRVA(va);
            va += (uint)table.Size;
        }
    }

    public void Add(PESectionData data) => Content.Add(data);

    public List<PESectionData>.Enumerator GetEnumerator() => Content.GetEnumerator();

    IEnumerator<PESectionData> IEnumerable<PESectionData>.GetEnumerator() => Content.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Content).GetEnumerator();
}