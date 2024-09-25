// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace LibObjectFile.IO;

/// <summary>
/// Provides indentation functionality for writing text to a <see cref="TextWriter"/>.
/// </summary>
public struct TextWriterIndenter
{
    private readonly TextWriter _writer;
    private int _indentLevel;
    private readonly int _indentSize;
    private bool _previousCharWasNewLine;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterIndenter"/> struct with the specified <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to write the indented text to.</param>
    public TextWriterIndenter(TextWriter writer)
    {
        _writer = writer;
        _indentSize = 4;
        _indentLevel = 0;
        _previousCharWasNewLine = true;
    }

    /// <summary>
    /// Gets or sets the size of the indentation.
    /// </summary>
    /// <value>The size of the indentation. The default value is 4.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified value is less than 0 or greater than 8.</exception>
    public int IndentSize
    {
        get => _indentSize;
        init
        {
            if (value < 0 || value > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "IndentSize must be between 0 and 8");
            }

            _indentSize = value;
        }
    }

    /// <summary>
    /// Writes the specified string to the underlying <see cref="TextWriter"/> without indenting.
    /// </summary>
    /// <param name="value">The string to write.</param>
    public void Write(string value)
    {
        WriteInternal(value);
    }

    /// <summary>
    /// Writes the specified span of characters to the underlying <see cref="TextWriter"/> without indenting.
    /// </summary>
    /// <param name="value">The span of characters to write.</param>
    public void Write(ReadOnlySpan<char> value)
    {
        WriteInternal(value);
    }

    /// <summary>
    /// Writes a new line to the underlying <see cref="TextWriter"/> with the current indentation level.
    /// </summary>
    public void WriteLine()
    {
        WriteIndent();
        _writer.WriteLine();
        _previousCharWasNewLine = true;
    }

    /// <summary>
    /// Writes the specified string to the underlying <see cref="TextWriter"/> with the current indentation level.
    /// </summary>
    /// <param name="value">The string to write.</param>
    public void WriteLine(string value)
    {
        WriteInternal(value);
        _writer.WriteLine();
        _previousCharWasNewLine = true;
    }

    /// <summary>
    /// Writes the specified span of characters to the underlying <see cref="TextWriter"/> with the current indentation level.
    /// </summary>
    /// <param name="value">The span of characters to write.</param>
    public void WriteLine(ReadOnlySpan<char> value)
    {
        WriteInternal(value);
        _writer.WriteLine();
        _previousCharWasNewLine = true;
    }

    /// <summary>
    /// Increases the indentation level by one.
    /// </summary>
    public void Indent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the indentation level by one.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when trying to unindent below 0.</exception>
    public void Unindent()
    {
        if (_indentLevel == 0)
        {
            throw new InvalidOperationException("Cannot unindent below 0");
        }
        _indentLevel--;
    }

    private void WriteInternal(ReadOnlySpan<char> data)
    {
        while (data.Length > 0)
        {
            WriteIndent();

            var nextEndOfLine = data.IndexOfAny('\r', '\n');
            if (nextEndOfLine < 0)
            {
                _writer.Write(data);
                break;
            }

            // Write the current line
            _writer.WriteLine(data.Slice(0, nextEndOfLine));
            _previousCharWasNewLine = true;

            // Move to the next line
            data = data.Slice(nextEndOfLine + 1);
            if (data.Length > 0 && data[0] == '\n')
            {
                data = data.Slice(1);
            }
        }
    }

    [SkipLocalsInit]
    private void WriteIndent()
    {
        if (_previousCharWasNewLine)
        {
            var indentSize = _indentLevel * IndentSize;
            if (indentSize > 0)
            {
                // Could be more optimized without requiring a stackalloc
                Span<char> buffer = stackalloc char[_indentLevel * IndentSize];
                buffer.Fill(' ');
                _writer.Write(buffer);
            }

            _previousCharWasNewLine = false;
        }
    }
}
