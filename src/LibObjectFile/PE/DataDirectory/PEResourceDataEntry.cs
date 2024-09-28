// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource data entry in a PE file.
/// </summary>
public sealed class PEResourceDataEntry : PEResourceEntry
{
    internal PEResourceDataEntry()
    {
        Data = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDataEntry"/> class.
    /// </summary>
    public PEResourceDataEntry(PEResourceData data)
    {
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceDataEntry"/> class.
    /// </summary>
    public PEResourceDataEntry(Encoding? codePage, PEResourceData data)
    {
        CodePage = codePage;
        Data = data;
    }
    
    /// <summary>
    /// Gets or sets the code page used for encoding the data.
    /// </summary>
    /// <remarks>
    /// The code page is used to encode the data when the data is a string.
    /// </remarks>
    public Encoding? CodePage { get; set; }

    /// <summary>
    /// Gets or sets the data associated with the resource data entry.
    /// </summary>
    /// <remarks>
    /// The data can be a string, a stream, or a byte array.
    /// </remarks>
    public PEResourceData Data { get; set; }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"{nameof(CodePage)} = {CodePage?.EncodingName}, {nameof(Data)} = {Data}");
        return true;
    }

    public override unsafe void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = (uint)sizeof(RawImageResourceDataEntry);
    }

    internal override unsafe void Read(in ReaderContext context)
    {
        var reader = context.Reader;

        reader.Position = Position;
        Size = (uint)sizeof(RawImageResourceDataEntry);

        RawImageResourceDataEntry rawDataEntry;
        if (!reader.TryReadData(sizeof(RawImageResourceDataEntry), out rawDataEntry))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntry, $"Invalid resource data entry at position {reader.Position}");
            return;
        }
        
        CodePage = rawDataEntry.CodePage != 0 ? Encoding.GetEncoding((int)rawDataEntry.CodePage) : null;

        var peFile = context.Reader.File;
        if (!peFile.TryFindSection(rawDataEntry.OffsetToData, out var section))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceDirectoryEntryRVAOffsetToData, $"Invalid resource data entry RVA OffsetToData {rawDataEntry.OffsetToData} at position {reader.Position}");
            return;
        }

        Data = new PEResourceData
        {
            Position = section.Position + rawDataEntry.OffsetToData - section.RVA,
            Size = rawDataEntry.Size,
        };

        // If we find that the position is not aligned on 4 bytes as we expect, reset it to 1 byte alignment
        var checkPosition = AlignHelper.AlignUp(Data.Position, Data.RequiredPositionAlignment);
        if (checkPosition != Data.Position)
        {
            Data.RequiredPositionAlignment = 1;
        }
        else if (context.ResourceDataList.Count == 0 && (Data.Position & 0xF) == 0)
        {
            // If we are the first resource data entry and the position is aligned on 16 bytes, we can assume this alignment
            Data.RequiredPositionAlignment = 16;
        }
        
        // Read the data
        Data.Read(reader);

        // Register the list of data being loaded
        context.ResourceDataList.Add(Data);
    }
}