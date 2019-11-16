namespace LibObjectFile.Elf
{
    public sealed class ElfSegment : ElfObjectFilePart
    {
        public ElfSegmentType Type { get; set; }

        public ElfSegmentRange Range { get; set; }

        public ulong VirtualAddress { get; set; }

        public ulong PhysicalAddress { get; set; }
        
        public ulong SizeInMemory { get; set; }

        public ElfSegmentFlags Flags { get; set; }

        public ulong Align { get; set; }

        protected override ulong GetSizeAuto() => Range.Size;

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            if (Range.IsEmpty)
            {
                //diagnostics.Error($"Invalid empty {nameof(Range)} in {this}. An {nameof(ElfSegment)} requires to be attached to a section or a range of section or a {nameof(ElfShadowSection)}");
            }
            else
            {
                if (Range.BeginSection.Parent == null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.BeginSection)} in {this}. The section must be attached to the same {nameof(ElfObjectFile)} than this instance");
                }

                if (Range.EndSection.Parent == null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.EndSection)} in {this}. The section must be attached to the same {nameof(ElfObjectFile)} than this instance");
                }

                if (Range.BeginOffset >= Range.BeginSection.Size)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginOffset, $"Invalid {nameof(Range)}.{nameof(Range.BeginOffset)}: {Range.BeginOffset} cannot be >= {nameof(Range.BeginSection)}.{nameof(ElfSection.Size)}: {Range.BeginSection.Size} in {this}. The offset must be within the section");
                }

                if ((Range.EndOffset >= 0 && (ulong)Range.EndOffset >= Range.EndSection.Size))
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndOffset, $"Invalid {nameof(Range)}.{nameof(Range.EndOffset)}: {Range.EndOffset} cannot be >= {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSection.Size)}: {Range.EndSection.Size} in {this}. The offset must be within the section");
                }
                else if (Range.EndOffset < 0)
                {
                    var endOffset = (long) Range.EndSection.Size + Range.EndOffset;
                    if (endOffset < 0)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndOffset, $"Invalid relative {nameof(Range)}.{nameof(Range.EndOffset)}: {Range.EndOffset}. The resulting end offset {endOffset} with {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSection.Size)}: {Range.EndSection.Size} cannot be < 0 in {this}. The offset must be within the section");
                    }
                }

                if (Range.BeginSection.Parent != null && Range.EndSection.Parent != null)
                {
                    if (Range.BeginSection.Index > Range.EndSection.Index)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeIndices, $"Invalid index order between {nameof(Range)}.{nameof(ElfSegmentRange.BeginSection)}.{nameof(ElfSegment.Index)}: {Range.BeginSection.Index} and {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSegment.Index)}: {Range.EndSection.Index} in {this}. The from index must be <= to the end index.");
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Segment [{Index}]";
        }
    }
}