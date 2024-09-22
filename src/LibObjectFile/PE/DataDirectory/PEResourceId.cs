// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource identifier in a PE file.
/// </summary>
/// <remarks>
/// This struct is used to identify different types of resources in a PE file.
/// It provides methods for comparing, converting to string, and retrieving well-known resource type names.
/// </remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public readonly struct PEResourceId : IEquatable<PEResourceId>, IComparable<PEResourceId>, IComparable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly int _id;

    private static readonly Dictionary<int, string> WellKnownResourceIDs = new Dictionary<int, string>()
    {
        { 1, "RT_CURSOR" },
        { 2, "RT_BITMAP" },
        { 3, "RT_ICON" },
        { 4, "RT_MENU" },
        { 5, "RT_DIALOG" },
        { 6, "RT_STRING" },
        { 7, "RT_FONTDIR" },
        { 8, "RT_FONT" },
        { 9, "RT_ACCELERATOR" },
        { 10, "RT_RCDATA" },
        { 11, "RT_MESSAGETABLE" },
        { 12, "RT_GROUP_CURSOR" },
        { 14, "RT_GROUP_ICON" },
        { 16, "RT_VERSION" },
        { 17, "RT_DLGINCLUDE" },
        { 19, "RT_PLUGPLAY" },
        { 20, "RT_VXD" },
        { 21, "RT_ANICURSOR" },
        { 22, "RT_ANIICON" },
        { 23, "RT_HTML" },
        { 24, "RT_MANIFEST" }
    };

    /// <summary>
    /// Represents the RT_CURSOR resource type.
    /// </summary>
    public static PEResourceId RT_CURSOR => new(1);

    /// <summary>
    /// Represents the RT_BITMAP resource type.
    /// </summary>
    public static PEResourceId RT_BITMAP => new(2);

    /// <summary>
    /// Represents the RT_ICON resource type.
    /// </summary>
    public static PEResourceId RT_ICON => new(3);

    /// <summary>
    /// Represents the RT_MENU resource type.
    /// </summary>
    public static PEResourceId RT_MENU => new(4);

    /// <summary>
    /// Represents the RT_DIALOG resource type.
    /// </summary>
    public static PEResourceId RT_DIALOG => new(5);

    /// <summary>
    /// Represents the RT_STRING resource type.
    /// </summary>
    public static PEResourceId RT_STRING => new(6);

    /// <summary>
    /// Represents the RT_FONTDIR resource type.
    /// </summary>
    public static PEResourceId RT_FONTDIR => new(7);

    /// <summary>
    /// Represents the RT_FONT resource type.
    /// </summary>
    public static PEResourceId RT_FONT => new(8);

    /// <summary>
    /// Represents the RT_ACCELERATOR resource type.
    /// </summary>
    public static PEResourceId RT_ACCELERATOR => new(9);

    /// <summary>
    /// Represents the RT_RCDATA resource type.
    /// </summary>
    public static PEResourceId RT_RCDATA => new(10);

    /// <summary>
    /// Represents the RT_MESSAGETABLE resource type.
    /// </summary>
    public static PEResourceId RT_MESSAGETABLE => new(11);

    /// <summary>
    /// Represents the RT_GROUP_CURSOR resource type.
    /// </summary>
    public static PEResourceId RT_GROUP_CURSOR => new(12);

    /// <summary>
    /// Represents the RT_GROUP_ICON resource type.
    /// </summary>
    public static PEResourceId RT_GROUP_ICON => new(14);

    /// <summary>
    /// Represents the RT_VERSION resource type.
    /// </summary>
    public static PEResourceId RT_VERSION => new(16);

    /// <summary>
    /// Represents the RT_DLGINCLUDE resource type.
    /// </summary>
    public static PEResourceId RT_DLGINCLUDE => new(17);

    /// <summary>
    /// Represents the RT_PLUGPLAY resource type.
    /// </summary>
    public static PEResourceId RT_PLUGPLAY => new(19);

    /// <summary>
    /// Represents the RT_VXD resource type.
    /// </summary>
    public static PEResourceId RT_VXD => new(20);

    /// <summary>
    /// Represents the RT_ANICURSOR resource type.
    /// </summary>
    public static PEResourceId RT_ANICURSOR => new(21);

    /// <summary>
    /// Represents the RT_ANIICON resource type.
    /// </summary>
    public static PEResourceId RT_ANIICON => new(22);

    /// <summary>
    /// Represents the RT_HTML resource type.
    /// </summary>
    public static PEResourceId RT_HTML => new(23);

    /// <summary>
    /// Represents the RT_MANIFEST resource type.
    /// </summary>
    public static PEResourceId RT_MANIFEST => new(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="PEResourceId"/> struct.
    /// </summary>
    /// <param name="id">The resource identifier.</param>
    public PEResourceId(int id)
    {
        _id = id;
    }

    /// <summary>
    /// Gets the value of the resource identifier.
    /// </summary>
    public int Value => _id;

    /// <summary>
    /// Converts the resource identifier to its hexadecimal string representation.
    /// </summary>
    /// <returns>The hexadecimal string representation of the resource identifier.</returns>
    public override string ToString() => $"0x{_id:X}";

    /// <summary>
    /// Tries to retrieve the well-known resource type name associated with the resource identifier.
    /// </summary>
    /// <param name="name">The well-known resource type name, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the well-known resource type name is found; otherwise, <c>false</c>.</returns>
    public bool TryGetWellKnownTypeName([NotNullWhen(true)] out string? name) => WellKnownResourceIDs.TryGetValue(_id, out name);

    /// <summary>
    /// Determines whether the current resource identifier is equal to another resource identifier.
    /// </summary>
    /// <param name="other">The resource identifier to compare with.</param>
    /// <returns><c>true</c> if the current resource identifier is equal to the other resource identifier; otherwise, <c>false</c>.</returns>
    public bool Equals(PEResourceId other) => _id == other._id;

    /// <summary>
    /// Determines whether the current resource identifier is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if the current resource identifier is equal to the other object; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is PEResourceId other && Equals(other);

    /// <summary>
    /// Gets the hash code of the resource identifier.
    /// </summary>
    /// <returns>The hash code of the resource identifier.</returns>
    public override int GetHashCode() => _id;

    /// <summary>
    /// Determines whether two resource identifiers are equal.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the two resource identifiers are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(PEResourceId left, PEResourceId right) => left.Equals(right);

    /// <summary>
    /// Determines whether two resource identifiers are not equal.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the two resource identifiers are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(PEResourceId left, PEResourceId right) => !left.Equals(right);

    /// <summary>
    /// Compares the current resource identifier with another resource identifier.
    /// </summary>
    /// <param name="other">The resource identifier to compare with.</param>
    /// <returns>A value indicating the relative order of the resource identifiers.</returns>
    public int CompareTo(PEResourceId other) => _id.CompareTo(other._id);

    /// <summary>
    /// Compares the current resource identifier with another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A value indicating the relative order of the resource identifiers.</returns>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is PEResourceId other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PEResourceId)}");
    }

    /// <summary>
    /// Determines whether one resource identifier is less than another resource identifier.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the first resource identifier is less than the second resource identifier; otherwise, <c>false</c>.</returns>
    public static bool operator <(PEResourceId left, PEResourceId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one resource identifier is greater than another resource identifier.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the first resource identifier is greater than the second resource identifier; otherwise, <c>false</c>.</returns>
    public static bool operator >(PEResourceId left, PEResourceId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one resource identifier is less than or equal to another resource identifier.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the first resource identifier is less than or equal to the second resource identifier; otherwise, <c>false</c>.</returns>
    public static bool operator <=(PEResourceId left, PEResourceId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one resource identifier is greater than or equal to another resource identifier.
    /// </summary>
    /// <param name="left">The first resource identifier to compare.</param>
    /// <param name="right">The second resource identifier to compare.</param>
    /// <returns><c>true</c> if the first resource identifier is greater than or equal to the second resource identifier; otherwise, <c>false</c>.</returns>
    public static bool operator >=(PEResourceId left, PEResourceId right) => left.CompareTo(right) >= 0;
}
