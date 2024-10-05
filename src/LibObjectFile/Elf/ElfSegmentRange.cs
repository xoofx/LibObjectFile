// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf;

/// <summary>
/// Defines the range of content a segment is bound to.
/// </summary>
public readonly struct ElfSegmentRange : IEquatable<ElfSegmentRange>
{
    public static readonly ElfSegmentRange Empty = new ElfSegmentRange();

    /// <summary>
    /// Creates a new instance that is bound to an entire content/
    /// </summary>
    /// <param name="content">The content to be bound to</param>
    public ElfSegmentRange(ElfContent content)
    {
        BeginContent = content ?? throw new ArgumentNullException(nameof(content));
        BeginOffset = 0;
        EndContent = content;
        OffsetFromEnd = 0;
    }

    /// <summary>
    /// Creates a new instance that is bound to a range of content.
    /// </summary>
    /// <param name="beginContent">The first content.</param>
    /// <param name="beginOffset">The offset inside the first content.</param>
    /// <param name="endContent">The last content.</param>
    /// <param name="offsetFromEnd">The offset in the last content</param>
    public ElfSegmentRange(ElfContent beginContent, ulong beginOffset, ElfContent endContent, ulong offsetFromEnd)
    {
        BeginContent = beginContent ?? throw new ArgumentNullException(nameof(beginContent));
        BeginOffset = beginOffset;
        EndContent = endContent ?? throw new ArgumentNullException(nameof(endContent));
        OffsetFromEnd = offsetFromEnd;
        if (BeginContent.Index > EndContent.Index)
        {
            throw new ArgumentOutOfRangeException(nameof(beginContent), $"The {nameof(beginContent)}.{nameof(ElfSection.Index)} = {BeginContent.Index} is > {nameof(endContent)}.{nameof(ElfSection.Index)} = {EndContent.Index}, while it must be <=");
        }
    }
        
    /// <summary>
    /// The first content.
    /// </summary>
    public readonly ElfContent? BeginContent;

    /// <summary>
    /// The relative offset in <see cref="BeginContent"/>.
    /// </summary>
    public readonly ulong BeginOffset;

    /// <summary>
    /// The last content.
    /// </summary>
    public readonly ElfContent? EndContent;

    /// <summary>
    /// The offset in the last content. If the offset is &lt; 0, then the actual offset starts from end of the content where finalEndOffset = content.Size + EndOffset.
    /// </summary>
    public readonly ulong OffsetFromEnd;

    /// <summary>
    /// Gets a boolean indicating if this content is empty.
    /// </summary>
    public bool IsEmpty => this == Empty;

    /// <summary>
    /// Returns the absolute offset of this range taking into account the <see cref="BeginContent"/>.<see cref="ObjectFileElement.Position"/>.
    /// </summary>
    public ulong Offset
    {
        get
        {
            // If this Begin/End content are not attached we can't calculate any meaningful size
            if (BeginContent?.Parent == null || EndContent?.Parent == null || BeginContent?.Parent != EndContent?.Parent)
            {
                return 0;
            }

            return BeginContent!.Position + BeginOffset;
        }
    }

    /// <summary>
    /// Returns the size of this range taking into account the size of each content involved in this range.
    /// </summary>
    public ulong Size
    {
        get
        {
            // If this Begin/End content are not attached we can't calculate any meaningful size
            if (BeginContent?.Parent == null || EndContent?.Parent == null || BeginContent.Parent != EndContent.Parent)
            {
                return 0;
            }

            ulong size = EndContent.Position + EndContent.Size - BeginContent.Position;
            size -= BeginOffset;
            size -= OffsetFromEnd;
            return size;
        }
    }
        
    public bool Equals(ElfSegmentRange other)
    {
        return Equals(BeginContent, other.BeginContent) && BeginOffset == other.BeginOffset && Equals(EndContent, other.EndContent) && OffsetFromEnd == other.OffsetFromEnd;
    }

    public override bool Equals(object? obj)
    {
        return obj is ElfSegmentRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (BeginContent != null ? BeginContent.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ BeginOffset.GetHashCode();
            hashCode = (hashCode * 397) ^ (EndContent != null ? EndContent.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ OffsetFromEnd.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ElfSegmentRange left, ElfSegmentRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ElfSegmentRange left, ElfSegmentRange right)
    {
        return !left.Equals(right);
    }

    public static implicit operator ElfSegmentRange(ElfContent? content)
    {
        return content is null ? Empty : new ElfSegmentRange(content);
    }
}