// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Represents an abstract base class for a PE resource entry.
/// </summary>
public abstract class PEResourceEntry : ObjectElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceEntry"/> class with a default ID of -1.
    /// </summary>
    private protected PEResourceEntry()
    {
        Id = new(-1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceEntry"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the resource entry.</param>
    protected PEResourceEntry(string? name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceEntry"/> class with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the resource entry.</param>
    protected PEResourceEntry(PEResourceId id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Value, 0, nameof(id));
        Id = id;
    }

    /// <summary>
    /// Gets the name of the resource entry.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the ID of the resource entry.
    /// </summary>
    public PEResourceId Id { get; }

    /// <summary>
    /// Gets a value indicating whether the resource entry is the root entry.
    /// </summary>
    public bool IsRoot => Id.Value < 0;

    /// <summary>
    /// Gets the level of the resource entry in the resource directory hierarchy.
    /// </summary>
    /// <returns>The level of the resource entry.</returns>
    public int GetLevel()
    {
        var level = 0;
        ObjectElement? parent = this;
        while (parent is not null && parent is not PEResourceDirectory)
        {
            parent = parent.Parent;
            level++;
        }

        if (parent is PEResourceDirectory)
        {
            level--;
        }
        else
        {
            level = -1;
        }

        return level;
    }

    /// <summary>
    /// Computes the full size of the resource entry.
    /// </summary>
    /// <returns>The full size of the resource entry.</returns>
    internal unsafe uint ComputeFullSize()
    {
        var size = Name != null ? (uint)Name.Length * 2 + sizeof(ushort) : 0;
        return (uint)(ComputeSize() + size + (IsRoot ? 0 : sizeof(RawImageResourceDirectoryEntry)));
    }

    /// <summary>
    /// Computes the size of the resource entry.
    /// </summary>
    /// <returns>The size of the resource entry.</returns>
    private protected abstract uint ComputeSize();


    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        if (!IsRoot)
        {
            if (Name != null)
            {
                builder.Append($"Name = {Name}");
            }
            else
            {
                builder.Append($"Id = {Id}");
                var level = GetLevel();
                if (level >= 0)
                {
                    switch (level)
                    {
                        case 1:
                            if (Id.TryGetWellKnownTypeName(out var name))
                            {
                                builder.Append($" ({name})");
                            }
                            break;
                        case 2:
                            break;
                        case 3:
                            try
                            {
                                var cultureInfo = CultureInfo.GetCultureInfo(Id.Value);
                                builder.Append($" ({cultureInfo.Name})");
                            }
                            catch (CultureNotFoundException)
                            {
                            }

                            break;
                    }
                }
            }

            return true;
        }

        return false;
    }
}
