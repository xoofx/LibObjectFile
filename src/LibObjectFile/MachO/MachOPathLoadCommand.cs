// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.MachO;

/// <summary>
/// Base class for the load commands that store a string inline after a fixed header.
/// </summary>
/// <remarks>
/// The string is stored NUL-terminated and then padded with more NULs until <c>cmdsize</c> is
/// aligned. The original <c>cmdsize</c> is kept as long as the string still fits, so re-writing
/// an untouched command reproduces the linker's padding byte for byte rather than the smallest
/// encoding this library would have chosen.
/// </remarks>
public abstract class MachOPathLoadCommand : MachOLoadCommand
{
    private string _value = string.Empty;

    /// <summary>
    /// Gets or sets the string carried by this command.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is null.</exception>
    public string Value
    {
        get => _value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _value = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the containing image is 64-bit, which sets the <c>cmdsize</c> alignment.
    /// </summary>
    public bool Is64Bit { get; set; }

    /// <summary>
    /// Gets the size of the fixed part preceding the string, including <c>cmd</c> and <c>cmdsize</c>.
    /// </summary>
    protected abstract uint FixedSize { get; }

    /// <summary>
    /// Gets the smallest valid <c>cmdsize</c> for the current <see cref="Value"/>.
    /// </summary>
    public uint MinimumSize
    {
        get
        {
            // The string is stored NUL-terminated, hence the extra byte.
            var unaligned = FixedSize + (uint)Encoding.UTF8.GetByteCount(Value) + 1;
            return AlignHelper.AlignUp(unaligned, GetSizeAlignment(Is64Bit));
        }
    }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context)
    {
        var minimum = MinimumSize;
        if (Size < minimum)
        {
            Size = minimum;
        }
    }

    /// <summary>
    /// Reads the string that follows the fixed part.
    /// </summary>
    /// <param name="reader">The reader, positioned anywhere.</param>
    /// <param name="commandPosition">The file offset of the start of this command.</param>
    /// <param name="stringOffset">The offset of the string from the start of this command.</param>
    protected void ReadValue(MachOReader reader, ulong commandPosition, uint stringOffset)
    {
        if (stringOffset < FixedSize || stringOffset >= Size)
        {
            reader.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_InvalidLoadCommandSize,
                $"The string offset {stringOffset} in {Type} at 0x{commandPosition:X} is outside the command, which is {Size} bytes");
            return;
        }

        var length = (int)(Size - stringOffset);
        var buffer = new byte[length];
        reader.Position = commandPosition + stringOffset;
        reader.ReadExactly(buffer);

        var end = Array.IndexOf(buffer, (byte)0);
        if (end < 0) end = length;
        Value = Encoding.UTF8.GetString(buffer, 0, end);
    }

    /// <summary>
    /// Writes the string and the NUL padding that brings the command up to <see cref="ObjectFileElement.Size"/>.
    /// </summary>
    /// <param name="writer">The writer, positioned at the end of the fixed part.</param>
    protected void WriteValue(MachOWriter writer)
    {
        var bytes = Encoding.UTF8.GetBytes(Value);
        writer.Write(bytes);
        writer.WriteZero((int)Size - (int)FixedSize - bytes.Length);
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, Value = {Value}");
        return true;
    }
}
