// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Numerics;
using System.Text;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// Defines a segment or program header.
/// </summary>
public sealed class ElfSegment : ElfObject
{
    public ElfSegment()
    {
        AdditionalData = [];
    }

    public ElfOffsetCalculationMode OffsetCalculationMode { get; set; }
        
    /// <summary>
    /// Gets or sets the type of this segment.
    /// </summary>
    public ElfSegmentType Type { get; set; }

    /// <summary>
    /// Gets or sets the range of section this segment applies to.
    /// It can applies to <see cref="ElfContentData"/>.
    /// </summary>
    public ElfSegmentRange Range { get; set; }

    /// <summary>
    /// Gets or sets the virtual address.
    /// </summary>
    public ulong VirtualAddress { get; set; }

    /// <summary>
    /// Gets or sets the physical address.
    /// </summary>
    public ulong PhysicalAddress { get; set; }
        
    /// <summary>
    /// Gets or sets the size in bytes occupied in memory by this segment.
    /// </summary>
    public ulong SizeInMemory { get; set; }

    /// <summary>
    /// Gets or sets the flags of this segment.
    /// </summary>
    public ElfSegmentFlags Flags { get; set; }

    /// <summary>
    /// Gets the alignment requirement of this section.
    /// </summary>
    public ulong Alignment { get; set; }

    /// <summary>
    /// Gets or sets the additional data stored in the header.
    /// </summary>
    public byte[] AdditionalData { get; set; }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        if (OffsetCalculationMode == ElfOffsetCalculationMode.Auto)
        {
            Position = Range.Offset;
        }
            
        if (Range.IsEmpty)
        {
            //diagnostics.Error($"Invalid empty {nameof(Range)} in {this}. An {nameof(ElfSegment)} requires to be attached to a section or a range of section or a {nameof(ElfShadowSection)}");
        }
        else
        {
            Size = Range.Size;

            // TODO: Add checks that Alignment is Power Of 2
            var alignment = Alignment == 0 ? Alignment = 1 : Alignment;
            if (!BitOperations.IsPow2(alignment))
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentAlignmentForLoad, $"Invalid segment alignment requirements: Alignment = {alignment} must be a power of 2");
            }

            if (Range.BeginContent?.Parent == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.BeginContent)} in {this}. The section must be attached to the same {nameof(ElfFile)} than this instance");
            }

            if (Range.EndContent?.Parent == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.EndContent)} in {this}. The section must be attached to the same {nameof(ElfFile)} than this instance");
            }

            if (Type == ElfSegmentTypeCore.Load)
            {
                //// Specs:
                //// As ‘‘Program Loading’’ later in this part describes, loadable process segments must have congruent values for p_vaddr and p_offset, modulo the page size.
                //// TODO: how to make this configurable?
                //if ((alignment % 4096) != 0)
                //{
                //    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentAlignmentForLoad, $"Invalid {nameof(ElfNative.PT_LOAD)} segment alignment requirements: {alignment} must be multiple of the Page Size {4096}");
                //}

                //var mod = (VirtualAddress - Range.Offset) & (alignment - 1);
                //if (mod != 0)
                //{
                //    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentVirtualAddressOrOffset, $"Invalid {nameof(ElfNative.PT_LOAD)} segment alignment requirements: (VirtualAddress - Range.Offset) & (Alignment - 1) == {mod}  while it must be == 0");
                //}
            }

            if (Size > 0)
            {
                var range = Range;
                if (range.BeginContent is null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginSectionParent, $"Invalid null {nameof(Range)}.{nameof(Range.BeginContent)} in {this}. The section must be attached to the same {nameof(ElfFile)} than this instance");
                }
                else if (range.BeginOffset >= range.BeginContent.Size)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginOffset, $"Invalid {nameof(Range)}.{nameof(Range.BeginOffset)}: {Range.BeginOffset} cannot be >= {nameof(Range.BeginContent)}.{nameof(ElfSection.Size)}: {range.BeginContent.Size} in {this}. The offset must be within the section");
                }

                if (range.EndContent is null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndSectionParent, $"Invalid null {nameof(Range)}.{nameof(Range.EndContent)} in {this}. The section must be attached to the same {nameof(ElfFile)} than this instance");
                }
                else if ((ulong)Range.OffsetFromEnd > range.EndContent.Size)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndOffset, $"Invalid {nameof(Range)}.{nameof(Range.OffsetFromEnd)}: {Range.OffsetFromEnd} cannot be >= {nameof(Range)}.{nameof(ElfSegmentRange.EndContent)}.{nameof(ElfSection.Size)}: {range.EndContent.Size} in {this}. The offset must be within the section");
                }
            }

            if (Range.BeginContent?.Parent != null && Range.EndContent?.Parent != null)
            {
                if (Range.BeginContent.Index > Range.EndContent.Index)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeIndices, $"Invalid index order between {nameof(Range)}.{nameof(ElfSegmentRange.BeginContent)}.{nameof(ElfSegment.Index)}: {Range.BeginContent.Index} and {nameof(Range)}.{nameof(ElfSegmentRange.EndContent)}.{nameof(ElfSegment.Index)}: {Range.EndContent.Index} in {this}. The from index must be <= to the end index.");
                }
            }
        }
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"[{Index}] ");
        base.PrintMembers(builder);
        return true;
    }
}