// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource directory entry with an ID in a PE file.
/// </summary>
/// <param name="Id">The identifier of the resource directory entry.</param>
/// <param name="Entry">The resource entry associated with the identifier.</param>
public readonly record struct PEResourceDirectoryEntryById(PEResourceId Id, PEResourceEntry Entry);