// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
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
    protected override unsafe uint ComputeHeaderSize(PELayoutContext context)
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
        {
            using var tempSpan = TempSpan<byte>.Create((int)size, out var span);
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
                    ReadEntriesARM(MemoryMarshal.Cast<byte, RawExceptionFunctionEntryARM>(span));
                    break;
            }
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
                if (!peFile.TryFindByRVA((RVA)(uint)entryX86.BeginAddress.RVO, out var beginAddressContainer))
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
                if (!peFile.TryFindByRVA((RVA)(uint)entryX86.EndAddress.RVO - 1, out var endAddressContainer))
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
                

                if (!peFile.TryFindByRVA((RVA)(uint)entryX86.UnwindInfoAddress.RVO, out var unwindInfoAddressContainer))
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
            else if (entry is PEExceptionFunctionEntryARM entryARM)
            {
                if (!peFile.TryFindByRVA((RVA)(uint)entryARM.BeginAddress.RVO, out var beginAddressContainer))
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
        var machine = writer.PEFile.CoffHeader.Machine;
        switch (machine)
        {
            case Machine.Amd64:
            case Machine.I386:
                WriteX86(writer);
                break;
            case Machine.Arm:
            case Machine.Arm64:
                WriteARM(writer);
                break;
            default:
                // We don't write the exception directory for other architectures
                return;
        }
    }

    private void WriteX86(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RawExceptionFunctionEntryX86>.Create(Entries.Count, out var span);
        
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            var entryX86 = (PEExceptionFunctionEntryX86)entry;
            ref var rawEntry = ref span[i];
            rawEntry.BeginAddress = (uint)entryX86.BeginAddress.RVA();
            rawEntry.EndAddress = (uint)entryX86.EndAddress.RVA();
            rawEntry.UnwindInfoAddress = (uint)entryX86.UnwindInfoAddress.RVA();
        }

        writer.Write(tempSpan);
    }

    private void WriteARM(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RawExceptionFunctionEntryARM>.Create(Entries.Count, out var span);

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            var entryArm = (PEExceptionFunctionEntryARM)entry;
            ref var rawEntry = ref span[i];
            rawEntry.BeginAddress = (uint)entryArm.BeginAddress.RVA();
            rawEntry.UnwindData = entryArm.UnwindData;
        }

        writer.Write(tempSpan);
    }

    private void ReadEntriesARM(Span<RawExceptionFunctionEntryARM> rawEntries)
    {
        foreach (ref var rawEntry in rawEntries)
        {
            // Create entries with links to data but encode the RVO as the RVA until we bind it to the actual section data
            var entry = new PEExceptionFunctionEntryARM(
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