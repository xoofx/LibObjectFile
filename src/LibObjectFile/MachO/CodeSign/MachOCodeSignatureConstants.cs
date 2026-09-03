// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO.CodeSign;

/// <summary>
/// Constants of the embedded code signature format.
/// </summary>
/// <remarks>
/// Every structure in a code signature is stored big-endian, unlike the rest of a Mach-O image,
/// because the format predates Apple's move to little-endian hardware and was never changed.
/// </remarks>
public static class MachOCodeSignatureConstants
{
    /// <summary>Magic of the outer blob holding all the others (<c>CSMAGIC_EMBEDDED_SIGNATURE</c>).</summary>
    public const uint EmbeddedSignatureMagic = 0xfade0cc0;

    /// <summary>Magic of a code directory (<c>CSMAGIC_CODEDIRECTORY</c>).</summary>
    public const uint CodeDirectoryMagic = 0xfade0c02;

    /// <summary>Magic of a requirement set (<c>CSMAGIC_REQUIREMENTS</c>).</summary>
    public const uint RequirementsMagic = 0xfade0c01;

    /// <summary>Magic of the blob wrapping a CMS signature (<c>CSMAGIC_BLOBWRAPPER</c>).</summary>
    public const uint BlobWrapperMagic = 0xfade0b01;

    /// <summary>Slot of the code directory (<c>CSSLOT_CODEDIRECTORY</c>).</summary>
    public const uint CodeDirectorySlot = 0;

    /// <summary>Slot of the bundle's <c>Info.plist</c> hash (<c>CSSLOT_INFOSLOT</c>).</summary>
    public const uint InfoSlot = 1;

    /// <summary>Slot of the requirement set (<c>CSSLOT_REQUIREMENTS</c>).</summary>
    public const uint RequirementsSlot = 2;

    /// <summary>Slot of the CMS signature (<c>CSSLOT_SIGNATURESLOT</c>).</summary>
    public const uint SignatureSlot = 0x10000;

    /// <summary>Code directory version understanding the executable segment fields (<c>CS_SUPPORTSEXECSEG</c>).</summary>
    public const uint CodeDirectoryVersionExecSeg = 0x20400;

    /// <summary>Size of the fixed part of a <see cref="CodeDirectoryVersionExecSeg"/> code directory.</summary>
    public const int CodeDirectoryHeaderSize = 88;

    /// <summary>The signature carries no CMS signer and is trusted only by its own hashes (<c>CS_ADHOC</c>).</summary>
    public const uint AdHocFlag = 0x0002;

    /// <summary>SHA-256 code hashes (<c>CS_HASHTYPE_SHA256</c>).</summary>
    public const byte HashTypeSha256 = 2;

    /// <summary>Size in bytes of a SHA-256 hash.</summary>
    public const int Sha256Size = 32;

    /// <summary>The signed pages are 4 KB, stored as the log2 of the size.</summary>
    public const byte PageSizeLog2 = 12;

    /// <summary>Size of a signed page, derived from <see cref="PageSizeLog2"/>.</summary>
    public const int PageSize = 1 << PageSizeLog2;

    /// <summary>The executable segment belongs to a main binary rather than a library (<c>CS_EXECSEG_MAIN_BINARY</c>).</summary>
    public const ulong ExecSegMainBinary = 0x1;

    /// <summary>Alignment the signature blob starts at inside <c>__LINKEDIT</c>.</summary>
    public const int SignatureAlignment = 16;
}
