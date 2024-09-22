// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines the revision of the security certificate.
/// </summary>
public enum PESecurityCertificateRevision : ushort
{
    /// <summary>
    /// Version 1, legacy version of the Win_Certificate structure. It is supported only for purposes of verifying legacy Authenticode signatures
    /// </summary>
    Revision1 = 0x0100,

    /// <summary>
    /// Version 2 is the current version of the Win_Certificate structure.
    /// </summary>
    Revision2 = 0x0200,
}