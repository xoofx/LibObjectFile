// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// A Virtual Address (VA) link in a Portable Executable (PE) image.
/// </summary>
public sealed class VALink
{
    public VALink(PEObject owner)
    {
        Owner = owner;
    }
    
    public PEObject Owner { get; }

    public PEObject? Container { get; set; }
    
    public RVO Offset { get; set; }

    public bool TryGetVA(out VA va)
    {
        va = default;
        if (Container is null)
        {
            return false;
        }

        var peFile = Owner.GetPEFile();
        if (peFile is null)
        {
            return false;
        }
     
        va = peFile.OptionalHeader.ImageBase + Container!.RVA + Offset;
        return true;
    }
    
    internal void SetTempAddress(PEImageReader reader, ulong va)
    {
        var file = reader.File;
        var rva = (RVA)(uint)(va - file.OptionalHeader.ImageBase);
        Container = PEStreamSectionData.Empty;
        Offset = (RVO)(uint)rva;
    }

    internal bool TryBind(PEImageReader reader, bool isEndOfAddress = false)
    {
        var file = reader.File;

        var rva = (RVA)(uint)Offset;
        if (!file.TryFindContainerByRVA(rva - (isEndOfAddress ? 1U : 0), out var container))
        {
            return false;
        }
        
        Container = container;
        Offset = (RVO)(uint)(rva - container.RVA);
        return true;
    }
    
    public override string ToString()
    {
        if (TryGetVA(out var va))
        {
            return $"{nameof(Container)} = {Container}, Offset = 0x{Offset:X}, VA = {va}";
        }

        return Container is not null ? $"{nameof(Container)} = {Container}, Offset = 0x{Offset:X}" : "<empty>";
    }
}

public record struct VA(ulong Value)
{
    public static implicit operator ulong(VA value) => value.Value;

    public static implicit operator VA(ulong value) => new(value);

    public void Write32(Span<byte> destination)
    {
        if (destination.Length < 4)
            throw new ArgumentOutOfRangeException(nameof(destination), "Destination length must be at least 4 bytes");

        Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(destination)) = (uint)Value;
    }

    public void Write64(Span<byte> destination)
    {
        if (destination.Length < 8)
            throw new ArgumentOutOfRangeException(nameof(destination), "Destination length must be at least 8 bytes");

        Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(destination)) = Value;
    }

    public override string ToString() => $"0x{Value:X}";
}