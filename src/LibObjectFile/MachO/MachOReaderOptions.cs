// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Options for reading a <see cref="MachOFile"/>.
/// </summary>
public sealed class MachOReaderOptions
{
    /// <summary>
    /// Gets or sets whether segment content is read as a view over the input stream instead of
    /// being copied into memory. The input stream then has to outlive the returned file.
    /// </summary>
    public bool UseSubStream { get; set; }
}
