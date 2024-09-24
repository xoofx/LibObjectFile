// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Debug type information. This value can be a known type <see cref="PEDebugKnownType"/> or a custom type.
/// </summary>
/// <param name="Value">The raw value of the type.</param>
public record struct PEDebugType(uint Value)
{
    /// <summary>
    /// Gets whether the type is a known type <see cref="PEDebugKnownType"/>.
    /// </summary>
    public bool IsKnownType => Value <= (uint)PEDebugKnownType.ExDllCharacteristics;

    /// <summary>
    /// Converts a known type to a debug type.
    /// </summary>
    /// <param name="value">The known type to convert.</param>
    public static implicit operator PEDebugType(PEDebugKnownType value) => new PEDebugType((uint)value);

    /// <inheritdoc />
    public override string ToString()
    {
        switch ((PEDebugKnownType)Value)
        {
            case PEDebugKnownType.Unknown:
                return "Unknown";
            case PEDebugKnownType.Coff:
                return "COFF";
            case PEDebugKnownType.CodeView:
                return "CodeView";
            case PEDebugKnownType.Fpo:
                return "FPO";
            case PEDebugKnownType.Misc:
                return "Misc";
            case PEDebugKnownType.Exception:
                return "Exception";
            case PEDebugKnownType.Fixup:
                return "Fixup";
            case PEDebugKnownType.OmapToSrc:
                return "OmapToSrc";
            case PEDebugKnownType.OmapFromSrc:
                return "OmapFromSrc";
            case PEDebugKnownType.Borland:
                return "Borland";
            case PEDebugKnownType.Reserved10:
                return "Reserved10";
            case PEDebugKnownType.Clsid:
                return "Clsid";
            case PEDebugKnownType.Repro:
                return "Repro";
            case PEDebugKnownType.EmbeddedRawData:
                return "EmbeddedRawData";
            case PEDebugKnownType.SymbolFileHash:
                return "SymbolFileHash";
            case PEDebugKnownType.ExDllCharacteristics:
                return "ExDllCharacteristics";
            case PEDebugKnownType.VCFeature:
                return "VCFeature";
            case PEDebugKnownType.POGO:
                return "POGO";
            case PEDebugKnownType.ILTCG:
                return "ILTCG";
            case PEDebugKnownType.MPX:
                return "MPX";
            case PEDebugKnownType.SPGO:
                return "SPGO";
            default:
                return $"0x{Value:X}";
        }
    }
}