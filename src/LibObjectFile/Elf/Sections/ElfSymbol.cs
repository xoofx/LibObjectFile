// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf;

/// <summary>
/// A symbol entry in the <see cref="ElfSymbolTable"/>
/// This is the value seen in <see cref="ElfNative.Elf32_Sym"/> or <see cref="ElfNative.Elf64_Sym"/>
/// </summary>
public record struct ElfSymbol
{
    /// <summary>
    /// Gets or sets the value associated to this symbol.
    /// </summary>
    public ulong Value { get; set; }

    /// <summary>
    /// Gets or sets the size of this symbol.
    /// </summary>
    public ulong Size { get; set; }

    /// <summary>
    /// Gets or sets the type of this symbol (e.g <see cref="ElfSymbolType.Function"/> or <see cref="ElfSymbolType.NoType"/>).
    /// </summary>
    public ElfSymbolType Type { get; set; }

    /// <summary>
    /// Get or sets the binding applying to this symbol (e.g <see cref="ElfSymbolBind.Global"/> or <see cref="ElfSymbolBind.Local"/>).
    /// </summary>
    public ElfSymbolBind Bind { get; set; }

    /// <summary>
    /// Gets or sets the visibility of this symbol (e.g <see cref="ElfSymbolVisibility.Hidden"/>)
    /// </summary>
    public ElfSymbolVisibility Visibility { get; set; }

    /// <summary>
    /// Gets or sets the associated section to this symbol.
    /// </summary>
    public ElfSectionLink SectionLink { get; set; }

    /// <summary>
    /// Gets or sets the name of this symbol.
    /// </summary>
    public ElfString Name { get; set; }

    public bool IsEmpty
    {
        get => Value == 0 && Size == 0 && Type == ElfSymbolType.NoType && Bind == (ElfSymbolBind)0 && Visibility == (ElfSymbolVisibility)0 && SectionLink.IsEmpty && Name.IsEmpty;
    }
}