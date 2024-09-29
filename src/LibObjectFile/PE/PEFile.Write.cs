// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
using LibObjectFile.Utils;

namespace LibObjectFile.PE;

partial class PEFile
{
    /// <summary>
    /// Writes this PE file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void Write(Stream stream)
    {
        if (!TryWrite(stream, out var diagnostics))
        {
            throw new ObjectFileException($"Invalid PE File", diagnostics);
        }
    }

    /// <summary>
    /// Tries to write this PE file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="diagnostics">The output diagnostics</param>
    /// <returns><c>true</c> if writing was successful. otherwise <c>false</c></returns>
    public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        
        var peWriter = new PEImageWriter(this, stream);
        diagnostics = peWriter.Diagnostics;

        var context = new PELayoutContext(this, diagnostics);

        Verify(context);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        UpdateLayout(context);
        if (diagnostics.HasErrors)
        {
            return false;
        }

        Write(peWriter);
        
        return !diagnostics.HasErrors;
    }

    public override unsafe void Write(PEImageWriter writer)
    {
        var context = new PELayoutContext(this, writer.Diagnostics);
        UpdateLayout(context);

        var position = 0U;

        // Update DOS header
        writer.Write(DosHeader);
        position += (uint)sizeof(PEDosHeader);

        // Write DOS stub
        writer.Write(_dosStub);
        position += (uint)_dosStub.Length;

        // Write extra DOS stub
        if (_dosStubExtra != null)
        {
            _dosStubExtra.CopyTo(writer.Stream);
            position += (uint)_dosStubExtra.Length;
        }

        var zeroSize = (int)((int)AlignHelper.AlignUp(position, 8) - (int)position);
        writer.WriteZero((int)zeroSize);
        position += (uint)zeroSize;

        // PE00 header
        writer.Write(PESignature.PE);
        position += sizeof(PESignature); // PE00 header

        // COFF header
        writer.Write(CoffHeader);
        position += (uint)sizeof(PECoffHeader);


        if (IsPE32)
        {
            RawImageOptionalHeader32 header32;
            header32.Common = OptionalHeader.OptionalHeaderCommonPart1;
            header32.Base32 = OptionalHeader.OptionalHeaderBase32;
            header32.Common2 = OptionalHeader.OptionalHeaderCommonPart2;
            header32.Size32 = OptionalHeader.OptionalHeaderSize32;
            header32.Common3 = OptionalHeader.OptionalHeaderCommonPart3;
            writer.Write(header32);
            position += (uint)sizeof(RawImageOptionalHeader32);
        }
        else
        {
            RawImageOptionalHeader64 header64;
            header64.Common = OptionalHeader.OptionalHeaderCommonPart1;
            header64.Base64 = OptionalHeader.OptionalHeaderBase64;
            header64.Common2 = OptionalHeader.OptionalHeaderCommonPart2;
            header64.Size64 = OptionalHeader.OptionalHeaderSize64;
            header64.Common3 = OptionalHeader.OptionalHeaderCommonPart3;
            writer.Write(header64);
            position += (uint)sizeof(RawImageOptionalHeader64);
        }


        // Update directories
        Directories.Write(writer, ref position);

        // Write Section Headers
        RawImageSectionHeader sectionHeader = default;
        foreach (var section in _sections)
        {
            section.Name.CopyTo(new Span<byte>(sectionHeader.Name, 8));
            sectionHeader.VirtualSize = section.VirtualSize;
            sectionHeader.RVA = section.RVA;
            sectionHeader.SizeOfRawData = (uint)section.Size;
            sectionHeader.PointerToRawData = (uint)section.Position;
            sectionHeader.Characteristics = section.Characteristics;
            writer.Write(sectionHeader);
            position += (uint)sizeof(RawImageSectionHeader);
        }

        // Data before sections
        foreach (var extraData in ExtraDataBeforeSections)
        {
            extraData.Write(writer);
            position += (uint)extraData.Size;
        }
        
        // Ensure that SectionAlignment is a multiple of FileAlignment
        zeroSize = (int)(AlignHelper.AlignUp(position, OptionalHeader.FileAlignment) - position);
        writer.WriteZero(zeroSize);
        position += (uint)zeroSize;

        // Write sections
        foreach (var section in _sections)
        {
            var span = CollectionsMarshal.AsSpan(section.Content.UnsafeList);
            for (var i = 0; i < span.Length; i++)
            {
                var data = span[i];
                if (data.Position != position)
                {
                    writer.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Current position {position} for data Section[{i}] in {section} does not match expecting position {data.Position}");
                    return;
                }

                if (data is PEDataDirectory directory)
                {
                    directory.WriteHeaderAndContent(writer);
                }
                else
                {
                    data.Write(writer);
                }

                position += (uint)data.Size;
            }

            zeroSize = (int)(AlignHelper.AlignUp(position, writer.PEFile.OptionalHeader.FileAlignment) - position);
            writer.WriteZero(zeroSize);
        }

        // Data after sections
        foreach (var extraData in ExtraDataAfterSections)
        {
            if (extraData.Position != position)
            {
                writer.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Current position {position} doest not match expecting position {extraData.Position}");
                return;
            }

            extraData.Write(writer);
            position += (uint)extraData.Size;
        }

        if (position != Size)
        {
            writer.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidInternalState, $"Generated size {position} does not match expecting size {Size}");
        }
    }
}