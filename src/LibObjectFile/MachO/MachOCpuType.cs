// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// CPU architecture of a Mach-O image, as stored in the <c>cputype</c> header field.
/// </summary>
/// <remarks>
/// The 64-bit variants are the 32-bit value with <see cref="Abi64"/> set, which is how a
/// reader tells <c>x86_64</c> from <c>i386</c> without consulting the magic.
/// </remarks>
public enum MachOCpuType : uint
{
    /// <summary>Marks a type as using the 64-bit ABI (<c>CPU_ARCH_ABI64</c>).</summary>
    Abi64 = 0x01000000,

    /// <summary>Motorola 68000 (<c>CPU_TYPE_MC680x0</c>).</summary>
    MC680x0 = 6,
    /// <summary>32-bit Intel, also known as <c>CPU_TYPE_I386</c> (<c>CPU_TYPE_X86</c>).</summary>
    X86 = 7,
    /// <summary>64-bit Intel (<c>CPU_TYPE_X86_64</c>).</summary>
    X86_64 = X86 | Abi64,
    /// <summary>32-bit ARM (<c>CPU_TYPE_ARM</c>).</summary>
    Arm = 12,
    /// <summary>64-bit ARM (<c>CPU_TYPE_ARM64</c>).</summary>
    Arm64 = Arm | Abi64,
    /// <summary>32-bit PowerPC (<c>CPU_TYPE_POWERPC</c>).</summary>
    PowerPC = 18,
    /// <summary>64-bit PowerPC (<c>CPU_TYPE_POWERPC64</c>).</summary>
    PowerPC64 = PowerPC | Abi64,
}
