// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines the NT signature for a PE image.
/// </summary>
public enum PESignature : uint
{
    /// <summary>
    /// PE00
    /// </summary>
    PE = 0x00004550,
}