// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines a link into a virtual addressable object at a specific virtual offset.
/// </summary>
/// <typeparam name="TVirtualAddressable">The type of the virtual addressable object.</typeparam>
/// <param name="Element">The virtual addressable object linked.</param>
/// <param name="OffsetInElement">The offset within this element.</param>
public record struct RVALink<TVirtualAddressable>(TVirtualAddressable? Element, uint OffsetInElement)
    where TVirtualAddressable : class, IVirtualAddressable
{
    /// <summary>
    /// Gets a value indicating whether this instance is null.
    /// </summary>
    public bool IsNull => Element == null;
    
    /// <summary>
    /// Gets the virtual address of within the element.
    /// </summary>
    public RVA VirtualAddress => (Element?.VirtualAddress ?? 0) + OffsetInElement;
}
