// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource string in a Portable Executable (PE) file.
/// </summary>
public sealed class PEResourceString : PESectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceString"/> class.
    /// </summary>
    public PEResourceString()
    {
        Text = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceString"/> class with the specified text.
    /// </summary>
    /// <param name="text">The resource string text.</param>
    public PEResourceString(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets the resource string text.
    /// </summary>
    public string Text { get; set; }

    /// <inheritdoc/>
    public override bool HasChildren => false;

    /// <inheritdoc/>
    public override void Read(PEImageReader reader)
    {
        var length = reader.ReadU16();
        using var tempSpan = TempSpan<char>.Create(length, out var span);
        reader.Position = Position;
        var read = reader.Read(tempSpan.AsBytes);
        if (read != length)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidResourceString, $"Invalid resource string length. Expected: {length}, Read: {read}");
        }
        Text = new string(span);

        // Update the size after reading the string
        Size = (uint)(sizeof(ushort) + length);
    }

    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
        // TODO: validate string length <= ushort.MaxValue
        writer.WriteU16((ushort)Text.Length);
        writer.Write(MemoryMarshal.Cast<char, byte>(Text.AsSpan()));
    }

    public override void UpdateLayout(PELayoutContext layoutContext)
    {
        Size = (uint)(sizeof(ushort) + Text.Length * sizeof(char));
    }

    /// <inheritdoc/>
    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"Text = {Text}");
        return true;
    }
    
    /// <inheritdoc/>
    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PEResourceDirectory)
        {
            throw new ArgumentException($"Invalid parent type {parent.GetType().FullName}. Expecting a parent of type {typeof(PEResourceDirectory).FullName}");
        }
    }
}
