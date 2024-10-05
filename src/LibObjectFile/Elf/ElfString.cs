// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// Defines a string with the associated index in the string table.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ElfString
{
    [Obsolete("This constructor cannot be used. Create an ElfString from a ElfStringTable", error: true)]
    public ElfString()
    {
        Value = string.Empty;
    }
    public ElfString(string text)
    {
        Value = text;
        Index = 0;
    }

    internal ElfString(string text, uint index)
    {
        Value = text;
        Index = index;
    }

    internal ElfString(uint index)
    {
        Value = string.Empty;
        Index = index;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value) && Index == 0;

    /// <summary>
    /// Gets the text of the string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the index of the string in the string table.
    /// </summary>
    public uint Index { get; }

    public override string ToString() => string.IsNullOrEmpty(Value) ? $"0x{Index:x8}" : Value;

    public static implicit operator ElfString(string text) => new(text);
}