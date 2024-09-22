﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the exception directory in a PE file.
/// </summary>
public sealed class PEExceptionDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEExceptionDirectory"/> class.
    /// </summary>
    public PEExceptionDirectory() : base(PEDataDirectoryKind.Exception)
    {
        Entries = new List<PEExceptionFunctionEntry>();
    }

    /// <summary>
    /// Gets the list of entries in the exception directory.
    /// </summary>
    public List<PEExceptionFunctionEntry> Entries { get; }

    /// <inheritdoc />
    protected override unsafe uint ComputeHeaderSize(PEVisitorContext context)
    {
        var machine = context.File.CoffHeader.Machine;
        uint entrySize;
        switch (machine)
        {
            case Machine.Amd64:
            case Machine.I386:
                entrySize = (uint)sizeof(RawExceptionFunctionEntryX86);
                break;
            case Machine.Arm:
            case Machine.Arm64:
                entrySize = (uint)sizeof(RawExceptionFunctionEntryARM);
                break;
            default:
                // We don't read the exception directory for other architectures
                // It will be added as raw content as part of this directory
                if (Entries.Count > 0)
                {
                    context.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entries, $"Unsupported entries in exception directory for machine {machine}");
                }
                return 0;
        }

        // TODO: Should be part of validate

        //foreach (var entry in Entries)
        //{
        //    switch (machine)
        //    {
        //        case Machine.Amd64:
        //        case Machine.I386:
        //            if (entry is not PEExceptionFunctionEntryX86)
        //            {
        //                context.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid entry {entry.GetType().Name} in exception directory for machine {machine}");
        //            }
        //            break;
        //        case Machine.Arm:
        //        case Machine.Arm64:
        //            if (entry is not PEExceptionFunctionEntryARM)
        //            {
        //                context.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid entry {entry.GetType().Name} in exception directory for machine {machine}");
        //            }
        //            break;
        //    }
        //}
        
        return entrySize * (uint)Entries.Count;
    }

    /// <inheritdoc />
    public override unsafe void Read(PEImageReader reader)
    {
        uint entrySize = 0;

        var machine = reader.File.CoffHeader.Machine;
        switch (machine)
        {
            case Machine.Amd64:
            case Machine.I386:
                entrySize = (uint)sizeof(RawExceptionFunctionEntryX86);
                break;
            case Machine.Arm:
            case Machine.Arm64:
                entrySize = (uint)sizeof(RawExceptionFunctionEntryARM);
                break;
            default:
                // We don't read the exception directory for other architectures
                // It will be added as raw content as part of this directory
                return;
        }

        var size = (long)Size;

        var (result, remainder) = Math.DivRem(size, entrySize);

        if (remainder != 0)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Size, $"Invalid size {size} for exception directory");
            return;
        }


        reader.Position = Position;

        var buffer = ArrayPool<byte>.Shared.Rent((int)size);
        try
        {
            var span = buffer.AsSpan().Slice(0, (int)size);
            int read = reader.Read(span);
            if (read != size)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Size, $"Invalid size {size} for exception directory");
                return;
            }

            switch (machine)
            {
                case Machine.Amd64:
                case Machine.I386:
                    ReadEntriesX86(MemoryMarshal.Cast<byte, RawExceptionFunctionEntryX86>(span));
                    break;
                case Machine.Arm:
                case Machine.Arm64:
                    ReadEntriesArm(MemoryMarshal.Cast<byte, RawExceptionFunctionEntryARM>(span));
                    break;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var headerSize = ComputeHeaderSize(reader);
        Debug.Assert(headerSize == size);
        HeaderSize = headerSize;
    }
    
    internal override void Bind(PEImageReader reader)
    {
        var peFile = reader.File;
        foreach (var entry in Entries)
        {
            if (entry is PEExceptionFunctionEntryX86 entryX86)
            {
                if (!peFile.TryFindContainerByRVA((RVA)(uint)entryX86.BeginAddress.RVO, out var beginAddressContainer))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid begin address {entryX86.BeginAddress.RVO} in exception directory");
                    return;
                }

                var beginAddressSectionData = beginAddressContainer as PESectionData;
                if (beginAddressSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid begin address {entryX86.BeginAddress.RVO} in exception directory. The container found is not a section data");
                    return;
                }

                // Need to subtract 1 to get the end address
                if (!peFile.TryFindContainerByRVA((RVA)(uint)entryX86.EndAddress.RVO - 1, out var endAddressContainer))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid end address {entryX86.EndAddress.RVO} in exception directory");
                    return;
                }

                var endAddressSectionData = endAddressContainer as PESectionData;
                if (endAddressSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid end address {entryX86.EndAddress.RVO} in exception directory. The container found is not a section data");
                    return;
                }
                

                if (!peFile.TryFindContainerByRVA((RVA)(uint)entryX86.UnwindInfoAddress.RVO, out var unwindInfoAddressContainer))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid unwind info address {entryX86.UnwindInfoAddress.RVO} in exception directory");
                    return;
                }

                var unwindInfoAddressSectionData = unwindInfoAddressContainer as PESectionData;
                if (unwindInfoAddressSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid unwind info address {entryX86.UnwindInfoAddress.RVO} in exception directory. The container found is not a section data");
                    return;
                }


                entryX86.BeginAddress = new PESectionDataLink(beginAddressSectionData, (uint)entryX86.BeginAddress.RVO - beginAddressSectionData.RVA);
                entryX86.EndAddress = new PESectionDataLink(endAddressSectionData, (uint)entryX86.EndAddress.RVO - endAddressSectionData.RVA);
                entryX86.UnwindInfoAddress = new PESectionDataLink(unwindInfoAddressSectionData, (uint)entryX86.UnwindInfoAddress.RVO - unwindInfoAddressSectionData.RVA);
            }
            else if (entry is PEExceptionFunctionEntryArm entryARM)
            {
                if (!peFile.TryFindContainerByRVA((RVA)(uint)entryARM.BeginAddress.RVO, out var beginAddressContainer))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid begin address {entryARM.BeginAddress.RVO} in exception directory");
                    return;
                }

                var beginAddressSectionData = beginAddressContainer as PESectionData;
                if (beginAddressSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_InvalidExceptionDirectory_Entry, $"Invalid begin address {entryARM.BeginAddress.RVO} in exception directory. The container found is not a section data");
                    return;
                }

                entryARM.BeginAddress = new PESectionDataLink(beginAddressSectionData, (uint)entryARM.BeginAddress.RVO - beginAddressSectionData.RVA);
            }
        }

    }

    /// <inheritdoc />
    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

    private void ReadEntriesArm(Span<RawExceptionFunctionEntryARM> rawEntries)
    {
        foreach (ref var rawEntry in rawEntries)
        {
            // Create entries with links to data but encode the RVO as the RVA until we bind it to the actual section data
            var entry = new PEExceptionFunctionEntryArm(
                new PESectionDataLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.BeginAddress),
                rawEntry.UnwindData
            );
            Entries.Add(entry);
        }
    }

    private void ReadEntriesX86(Span<RawExceptionFunctionEntryX86> rawEntries)
    {
        foreach (ref var rawEntry in rawEntries)
        {
            // Create entries with links to data but encode the RVO as the RVA until we bind it to the actual section data
            var entry = new PEExceptionFunctionEntryX86(
                new PESectionDataLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.BeginAddress),
                new PESectionDataLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.EndAddress),
                new PESectionDataLink(PEStreamSectionData.Empty, (RVO)(uint)rawEntry.UnwindInfoAddress)
            );
            Entries.Add(entry);
        }
    }
}