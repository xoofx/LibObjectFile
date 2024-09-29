// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents a resource directory entry with a name in a PE file.
/// </summary>
/// <param name="Name">The name of the resource directory entry.</param>
/// <param name="Entry">The resource entry associated with the name.</param>
public readonly record struct PEResourceDirectoryEntryByName(PEResourceString Name, PEResourceEntry Entry);