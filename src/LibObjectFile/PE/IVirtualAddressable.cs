// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Interface for an object that has a <see cref="RVA"/> virtual address.
/// </summary>
public interface IVirtualAddressable
{
    /// <summary>
    /// Gets the virtual address of this object.
    /// </summary>
    RVA VirtualAddress { get; }
}