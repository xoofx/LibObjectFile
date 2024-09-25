// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
//using System.Security.Cryptography.Pkcs;

namespace LibObjectFile.PE;

/// <summary>
/// Represents a security certificate used in a Portable Executable (PE) file available in <see cref="PECPESecurityCertificateDirectoryrtificates"/>.
/// </summary>
public sealed class PESecurityCertificate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PESecurityCertificate"/> class.
    /// </summary>
    public PESecurityCertificate()
    {
        Data = Stream.Null;
        Revision = PESecurityCertificateRevision.Revision2;
        Type = PESecurityCertificateType.PKCS7;
    }

    /// <summary>
    /// Gets or sets the revision of the security certificate.
    /// </summary>
    public PESecurityCertificateRevision Revision { get; set; }

    /// <summary>
    /// Gets or sets the type of the security certificate.
    /// </summary>
    public PESecurityCertificateType Type { get; set; }

    /// <summary>
    /// Gets or sets the data stream of the security certificate.
    /// </summary>
    public Stream Data { get; set; }

    ///// <summary>
    ///// Decodes the security certificate and returns a <see cref="SignedCms"/> object.
    ///// </summary>
    ///// <returns>The decoded <see cref="SignedCms"/> object.</returns>
    //public SignedCms Decode()
    //{
    //    SignedCms signedCms = new SignedCms();
    //    var stream = new MemoryStream();
    //    Data.Position = 0;
    //    Data.CopyTo(stream);

    //    signedCms.Decode(stream.ToArray());

    //    // Optionally verify the signature
    //    signedCms.CheckSignature(true);  // true to check certificate validity

    //    return signedCms;
    //}

    /// <summary>
    /// Returns a string representation of the security certificate.
    /// </summary>
    /// <returns>A string representation of the security certificate.</returns>
    public override string ToString()
    {
        return $"PECertificate {Type} {Revision}";
    }
}
