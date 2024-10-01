// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

internal struct RawImageOptionalHeaderBase32
{
    /// <summary>
    /// The address relative to the image base of the beginning of the data section.
    /// </summary>
    public RVA BaseOfData;

    //
    // NT additional fields.
    //

    /// <summary>
    /// The preferred address of the first byte of the image when loaded into memory.
    /// </summary>
    public uint ImageBase;
}