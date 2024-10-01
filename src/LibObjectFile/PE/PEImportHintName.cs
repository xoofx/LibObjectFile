// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// A PE Import Hint Name used in <see cref="PEImportFunctionEntry"/>.
/// </summary>
/// <param name="Hint">An index into the export name pointer table. A match is attempted first with this value. If it fails, a binary search is performed on the DLL's export name pointer table.</param>
/// <param name="Name">This is the string that must be matched to the public name in the DLL</param>
public readonly record struct PEImportHintName(ushort Hint, string? Name);