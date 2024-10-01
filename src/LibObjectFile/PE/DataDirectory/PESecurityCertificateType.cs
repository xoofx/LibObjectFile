// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines the type of the security certificate.
/// </summary>
public enum PESecurityCertificateType : ushort
{
    /// <summary>
    /// X.509 Certificate. Not Supported
    /// </summary>
    X509 = 0x0001,

    /// <summary>
    /// PKCS#7 SignedData structure.
    /// </summary>
    PKCS7 = 0x0002,

    /// <summary>
    /// Reserved. Not supported.
    /// </summary>
    Reserved = 0x0003,

    /// <summary>
    /// Terminal Server Protocol Stack Certificate signing. Not Supported
    /// </summary>
    TerminalServerProtocolStack = 0x0004,
}