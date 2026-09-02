// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace LibObjectFile.MachO.Internal;

/// <summary>
/// Helpers for the fixed 16-byte name fields used by segments and sections.
/// </summary>
internal static class MachOName
{
    /// <summary>
    /// The width of a segment or section name field.
    /// </summary>
    public const int Length = 16;

    /// <summary>
    /// Decodes a fixed-width name. A name that fills the field has no terminator, so the
    /// terminator is optional rather than required.
    /// </summary>
    public static string Read(ReadOnlySpan<byte> span)
    {
        int end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        return Encoding.UTF8.GetString(span.Slice(0, end));
    }

    /// <summary>
    /// Encodes a name into a fixed-width field, zero-filling the remainder. Names longer than
    /// the field are truncated, which matches what the linker does.
    /// </summary>
    public static void Write(Span<byte> span, string name)
    {
        span.Clear();
        Encoding.UTF8.TryGetBytes(name, span, out _);
    }
}
