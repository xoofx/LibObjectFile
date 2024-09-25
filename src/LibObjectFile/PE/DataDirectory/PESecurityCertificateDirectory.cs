// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the security directory in a PE file.
/// </summary>
public sealed class PESecurityCertificateDirectory : PEExtraData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PESecurityCertificateDirectory"/> class.
    /// </summary>
    public PESecurityCertificateDirectory()
    {
        Certificates = new();
    }

    /// <summary>
    /// Gets the list of certificates.
    /// </summary>
    public List<PESecurityCertificate> Certificates { get; }
    
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        long size = (long)Size;


        while (size > 0)
        {
            if (!reader.TryReadData(sizeof(RawCertificateHeader), out RawCertificateHeader header))
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Invalid certificate header at position {reader.Position}");
                return;
            }

            if (header.Length < sizeof(RawCertificateHeader) || header.Length > size)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidCertificateEntry, $"Invalid certificate length {header.Length} at position {reader.Position}");
                return;
            }

            if (header.Revision != PESecurityCertificateRevision.Revision2 && header.Revision != PESecurityCertificateRevision.Revision1)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidCertificateEntry, $"Invalid certificate version {header.Revision} at position {reader.Position}");
                return;
            }

            // Check the certificate type
            switch (header.Type)
            {
                case PESecurityCertificateType.X509:
                case PESecurityCertificateType.PKCS7:
                case PESecurityCertificateType.Reserved:
                case PESecurityCertificateType.TerminalServerProtocolStack:
                    break;
                default:
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidCertificateEntry, $"Unsupported certificate type {header.Type} at position {reader.Position}");
                    return;
            }

            var certificate = new PESecurityCertificate
            {
                Revision = header.Revision,
                Type = header.Type,
            };

            certificate.Data = reader.ReadAsStream(header.Length - 8);
            Certificates.Add(certificate);

            if (reader.HasErrors)
            {
                return;
            }
            
            size -= header.Length;

            // Make sure that we are aligned on the next entry
            reader.Position = AlignHelper.AlignUp(reader.Position, 8);
        }

        var computedSize = ComputeHeaderSize(reader);
        Debug.Assert(computedSize == Size);
    }
    
    private uint ComputeHeaderSize(PEVisitorContext context)
    {
        var size = 0u;
        foreach (var certificate in Certificates)
        {
            size += 8; // CertificateSize + Version + Type
            size += (uint)certificate.Data.Length;
            size = AlignHelper.AlignUp(size, 8U);
        }

        return size;
    }
    
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
    
    private struct RawCertificateHeader
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public uint Length;
        public PESecurityCertificateRevision Revision;
        public PESecurityCertificateType Type;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }
}