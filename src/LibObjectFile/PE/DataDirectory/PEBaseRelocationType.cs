// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents the different relocation types for an image file.
/// </summary>
public enum PEBaseRelocationType : ushort
{
    /// <summary>
    /// The base relocation is skipped. This type can be used to pad a block.
    /// </summary>
    Absolute = 0,

    /// <summary>
    /// The base relocation adds the high 16 bits of the difference to the 16-bit field at offset.
    /// The 16-bit field represents the high value of a 32-bit word.
    /// </summary>
    High = 1 << 12,

    /// <summary>
    /// The base relocation adds the low 16 bits of the difference to the 16-bit field at offset.
    /// The 16-bit field represents the low half of a 32-bit word.
    /// </summary>
    Low = 2 << 12,

    /// <summary>
    /// The base relocation applies all 32 bits of the difference to the 32-bit field at offset.
    /// </summary>
    HighLow = 3 << 12,

    /// <summary>
    /// The base relocation adds the high 16 bits of the difference to the 16-bit field at offset.
    /// The 16-bit field represents the high value of a 32-bit word. The low 16 bits of the 32-bit value 
    /// are stored in the 16-bit word that follows this base relocation. This means that this base 
    /// relocation occupies two slots.
    /// </summary>
    HighAdj = 4 << 12,

    /// <summary>
    /// When the machine type is MIPS, the base relocation applies to a MIPS jump instruction.
    /// When the machine type is ARM or Thumb, the base relocation applies the 32-bit address of a symbol across a consecutive MOVW/MOVT instruction pair.
    /// When the machine type is RISC-V, the base relocation applies to the high 20 bits of a 32-bit absolute address.
    /// </summary>
    MipsJmpAddrOrArmMov32OrRiscvHigh20 = 5 << 12,

    /// <summary>
    /// Reserved, must be zero.
    /// </summary>
    Reserved = 6 << 12,

    /// <summary>
    /// When the machine type is Thumb, the base relocation applies the 32-bit address of a symbol to a consecutive MOVW/MOVT instruction pair.
    /// When the machine type is RISC-V, the base relocation applies to the low 12 bits of a 32-bit absolute address formed in RISC-V I-type instruction format.
    /// </summary>
    ThumbMov32OrRiscvLow12I = 7 << 12,

    /// <summary>
    /// When the machine type is RISC-V, the base relocation applies to the low 12 bits of a 32-bit absolute address formed in RISC-V S-type instruction format.
    /// When the machine type is LoongArch 32-bit, the base relocation applies to a 32-bit absolute address formed in two consecutive instructions.
    /// When the machine type is LoongArch 64-bit, the base relocation applies to a 64-bit absolute address formed in four consecutive instructions.
    /// </summary>
    RiscvLow12SOrLoongArchMarkLa = 8 << 12,

    /// <summary>
    /// When the machine type is MIPS, the base relocation applies to a MIPS16 jump instruction.
    /// </summary>
    MipsJmpAddr16 = 9 << 12,

    /// <summary>
    /// The base relocation applies the difference to the 64-bit field at offset.
    /// </summary>
    Dir64 = 10 << 12
}