// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibObjectFile.PE;

public static class PEPrinter
{
    public static void Print(this PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);
        
        var indenter = new TextWriterIndenter(writer);

        PrintHeaders(file, ref indenter);
        PrintDataDirectories(file, ref indenter);
        PrintSections(file, ref indenter);
    }

    private static void PrintHeaders(PEFile file, ref TextWriterIndenter writer)
    {
        PrintDosHeader(file, ref writer);
        PrintDosStub(file, ref writer);
        PrintCoffHeader(file, ref writer);
        PrintOptionalHeader(file, ref writer);
    }

    private static unsafe void PrintDosHeader(PEFile file, ref TextWriterIndenter writer)
    {
        const int indent = -26;
        writer.WriteLine("DOS Header");
        writer.Indent();
        {
            writer.WriteLine($"{nameof(PEDosHeader.Magic),indent} = {file.DosHeader.Magic}");
            writer.WriteLine($"{nameof(PEDosHeader.ByteCountOnLastPage),indent} = 0x{file.DosHeader.ByteCountOnLastPage:X}");
            writer.WriteLine($"{nameof(PEDosHeader.PageCount),indent} = 0x{file.DosHeader.PageCount:X}");
            writer.WriteLine($"{nameof(PEDosHeader.RelocationCount),indent} = 0x{file.DosHeader.RelocationCount:X}");
            writer.WriteLine($"{nameof(PEDosHeader.SizeOfParagraphsHeader),indent} = 0x{file.DosHeader.SizeOfParagraphsHeader:X}");
            writer.WriteLine($"{nameof(PEDosHeader.MinExtraParagraphs),indent} = 0x{file.DosHeader.MinExtraParagraphs:X}");
            writer.WriteLine($"{nameof(PEDosHeader.MaxExtraParagraphs),indent} = 0x{file.DosHeader.MaxExtraParagraphs:X}");
            writer.WriteLine($"{nameof(PEDosHeader.InitialSSValue),indent} = 0x{file.DosHeader.InitialSSValue:X}");
            writer.WriteLine($"{nameof(PEDosHeader.InitialSPValue),indent} = 0x{file.DosHeader.InitialSPValue:X}");
            writer.WriteLine($"{nameof(PEDosHeader.Checksum),indent} = 0x{file.DosHeader.Checksum:X}");
            writer.WriteLine($"{nameof(PEDosHeader.InitialIPValue),indent} = 0x{file.DosHeader.InitialIPValue:X}");
            writer.WriteLine($"{nameof(PEDosHeader.InitialCSValue),indent} = 0x{file.DosHeader.InitialCSValue:X}");
            writer.WriteLine($"{nameof(PEDosHeader.FileAddressRelocationTable),indent} = 0x{file.DosHeader.FileAddressRelocationTable:X}");
            writer.WriteLine($"{nameof(PEDosHeader.OverlayNumber),indent} = 0x{file.DosHeader.OverlayNumber:X}");
            writer.WriteLine($"{nameof(PEDosHeader.Reserved),indent} = 0x{file.DosHeader.Reserved[0]:X}, 0x{file.DosHeader.Reserved[1]:X}, 0x{file.DosHeader.Reserved[2]:X}, 0x{file.DosHeader.Reserved[3]:X}");
            writer.WriteLine($"{nameof(PEDosHeader.OEMIdentifier),indent} = 0x{file.DosHeader.OEMIdentifier:X}");
            writer.WriteLine($"{nameof(PEDosHeader.OEMInformation),indent} = 0x{file.DosHeader.OEMInformation:X}");
            writer.WriteLine(
                $"{nameof(PEDosHeader.Reserved2),indent} = 0x{file.DosHeader.Reserved2[0]:X}, 0x{file.DosHeader.Reserved2[1]:X}, 0x{file.DosHeader.Reserved2[2]:X}, 0x{file.DosHeader.Reserved2[3]:X}, 0x{file.DosHeader.Reserved2[4]:X}, 0x{file.DosHeader.Reserved2[5]:X}, 0x{file.DosHeader.Reserved2[6]:X}, 0x{file.DosHeader.Reserved2[7]:X}, 0x{file.DosHeader.Reserved2[8]:X}, 0x{file.DosHeader.Reserved2[9]:X}");
            writer.WriteLine($"{nameof(PEDosHeader.FileAddressPEHeader),indent} = 0x{file.DosHeader.FileAddressPEHeader:X}");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintDosStub(PEFile file, ref TextWriterIndenter writer)
    {
        const int indent = -26;
        writer.WriteLine("DOS Stub");
        writer.Indent();
        {
            writer.WriteLine($"{nameof(file.DosStub),indent} = {file.DosStub.Length} bytes");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintCoffHeader(PEFile file, ref TextWriterIndenter writer)
    {
        const int indent = -26;

        writer.WriteLine("COFF Header");
        writer.Indent();
        {
            writer.WriteLine($"{nameof(PECoffHeader.Machine),indent} = {file.CoffHeader.Machine}");
            writer.WriteLine($"{nameof(PECoffHeader.NumberOfSections),indent} = {file.CoffHeader.NumberOfSections}");
            writer.WriteLine($"{nameof(PECoffHeader.TimeDateStamp),indent} = {file.CoffHeader.TimeDateStamp}");
            writer.WriteLine($"{nameof(PECoffHeader.PointerToSymbolTable),indent} = 0x{file.CoffHeader.PointerToSymbolTable:X}");
            writer.WriteLine($"{nameof(PECoffHeader.NumberOfSymbols),indent} = {file.CoffHeader.NumberOfSymbols}");
            writer.WriteLine($"{nameof(PECoffHeader.SizeOfOptionalHeader),indent} = {file.CoffHeader.SizeOfOptionalHeader}");
            writer.WriteLine($"{nameof(PECoffHeader.Characteristics),indent} = {file.CoffHeader.Characteristics}");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintOptionalHeader(PEFile file, ref TextWriterIndenter writer)
    {
        const int indent = -26;
        writer.WriteLine("Optional Header");
        writer.Indent();
        {
            writer.WriteLine($"{nameof(PEOptionalHeader.Magic),indent} = {file.OptionalHeader.Magic}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MajorLinkerVersion),indent} = {file.OptionalHeader.MajorLinkerVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MinorLinkerVersion),indent} = {file.OptionalHeader.MinorLinkerVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfCode),indent} = 0x{file.OptionalHeader.SizeOfCode:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfInitializedData),indent} = 0x{file.OptionalHeader.SizeOfInitializedData:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfUninitializedData),indent} = 0x{file.OptionalHeader.SizeOfUninitializedData:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.AddressOfEntryPoint),indent} = {file.OptionalHeader.AddressOfEntryPoint}");
            writer.WriteLine($"{nameof(PEOptionalHeader.BaseOfCode),indent} = {file.OptionalHeader.BaseOfCode}");
            writer.WriteLine($"{nameof(PEOptionalHeader.BaseOfData),indent} = 0x{file.OptionalHeader.BaseOfData:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.ImageBase),indent} = 0x{file.OptionalHeader.ImageBase:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SectionAlignment),indent} = 0x{file.OptionalHeader.SectionAlignment:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.FileAlignment),indent} = 0x{file.OptionalHeader.FileAlignment:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MajorOperatingSystemVersion),indent} = {file.OptionalHeader.MajorOperatingSystemVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MinorOperatingSystemVersion),indent} = {file.OptionalHeader.MinorOperatingSystemVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MajorImageVersion),indent} = {file.OptionalHeader.MajorImageVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MinorImageVersion),indent} = {file.OptionalHeader.MinorImageVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MajorSubsystemVersion),indent} = {file.OptionalHeader.MajorSubsystemVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.MinorSubsystemVersion),indent} = {file.OptionalHeader.MinorSubsystemVersion}");
            writer.WriteLine($"{nameof(PEOptionalHeader.Win32VersionValue),indent} = 0x{file.OptionalHeader.Win32VersionValue:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfImage),indent} = 0x{file.OptionalHeader.SizeOfImage:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfHeaders),indent} = 0x{file.OptionalHeader.SizeOfHeaders:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.CheckSum),indent} = 0x{file.OptionalHeader.CheckSum:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.Subsystem),indent} = {file.OptionalHeader.Subsystem}");
            writer.WriteLine($"{nameof(PEOptionalHeader.DllCharacteristics),indent} = {file.OptionalHeader.DllCharacteristics}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfStackReserve),indent} = 0x{file.OptionalHeader.SizeOfStackReserve:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfStackCommit),indent} = 0x{file.OptionalHeader.SizeOfStackCommit:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfHeapReserve),indent} = 0x{file.OptionalHeader.SizeOfHeapReserve:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.SizeOfHeapCommit),indent} = 0x{file.OptionalHeader.SizeOfHeapCommit:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.LoaderFlags),indent} = 0x{file.OptionalHeader.LoaderFlags:X}");
            writer.WriteLine($"{nameof(PEOptionalHeader.NumberOfRvaAndSizes),indent} = 0x{file.OptionalHeader.NumberOfRvaAndSizes:X}");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintDataDirectories(PEFile file, ref TextWriterIndenter writer)
    {
        if (file.Directories.Count == 0) return;

        writer.WriteLine("Data Directories");
        writer.Indent();
        for(int i = 0; i < file.Directories.Count; i++)
        {
            var kind = (PEDataDirectoryKind)i;
            var directory = file.Directories[kind];
            writer.WriteLine(directory is null 
                ? $"[{i:00}] = null" 
                : $"[{i:00}] = {PEDescribe(directory)}");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintSections(PEFile file, ref TextWriterIndenter writer)
    {
        writer.WriteLine("Section Headers");
        writer.Indent();
        for (var i = 0; i < file.Sections.Count; i++)
        {
            var section = file.Sections[i];
            writer.WriteLine($"[{i:00}] {section.Name,8} {PEDescribe(section)}, Characteristics = 0x{(uint)section.Characteristics:X8} ({section.Characteristics})");
        }
        writer.Unindent();
        writer.WriteLine();

        writer.WriteLine("Sections");

        string heading = new string('-', 224);
        writer.Indent();
        for (var i = 0; i < file.Sections.Count; i++)
        {
            var section = file.Sections[i];

            writer.WriteLine(heading);
            writer.WriteLine($"[{i:00}] {section.Name,8} {PEDescribe(section)}, Characteristics = 0x{(uint)section.Characteristics:X8} ({section.Characteristics})");
            writer.WriteLine();
            if (section.Content.Count > 0)
            {
                foreach (var data in section.Content)
                {
                    writer.Indent();
                    PrintSectionData(file, data, ref writer);
                    writer.Unindent();
                }
            }
        }
        writer.Unindent();
    }

    private static void PrintSectionData(PEFile file, PESectionData data, ref TextWriterIndenter writer)
    {
        writer.WriteLine($"[{data.Index:00}] {PEDescribe(data)}");
        writer.Indent();
        switch (data)
        {
            case PEBaseRelocationDirectory peBaseRelocationDirectory:
                Print(peBaseRelocationDirectory, ref writer);
                break;
            case PEBaseRelocationBlock baseRelocationBlock:
                Print(baseRelocationBlock, ref writer);
                break;
            case PEBoundImportDirectory peBoundImportDirectory:
                Print(peBoundImportDirectory, ref writer);
                break;
            case PEClrMetadata peClrMetadata:
                Print(peClrMetadata, ref writer);
                break;
            case PEArchitectureDirectory peArchitectureDirectory:
                Print(peArchitectureDirectory, ref writer);
                break;
            case PEDebugDirectory peDebugDirectory:
                Print(peDebugDirectory, ref writer);
                break;
            case PEDelayImportDirectory peDelayImportDirectory:
                Print(peDelayImportDirectory, ref writer);
                break;
            case PEExceptionDirectory peExceptionDirectory:
                Print(peExceptionDirectory, ref writer);
                break;
            case PEExportDirectory peExportDirectory:
                Print(peExportDirectory, ref writer);
                break;
            case PEGlobalPointerDirectory peGlobalPointerDirectory:
                Print(peGlobalPointerDirectory, ref writer);
                break;
            case PEImportAddressTableDirectory peImportAddressTableDirectory:
                Print(peImportAddressTableDirectory, ref writer);
                break;
            case PEImportDirectory peImportDirectory:
                Print(peImportDirectory, ref writer);
                break;
            case PELoadConfigDirectory32 peLoadConfigDirectory:
                Print(peLoadConfigDirectory, ref writer);
                break;
            case PELoadConfigDirectory64 peLoadConfigDirectory:
                Print(peLoadConfigDirectory, ref writer);
                break;
            case PEResourceDirectory peResourceDirectory:
                Print(peResourceDirectory, ref writer);
                break;
            case PETlsDirectory32 peTlsDirectory32:
                Print(peTlsDirectory32, ref writer);
                break;
            case PETlsDirectory64 peTlsDirectory64:
                Print(peTlsDirectory64, ref writer);
                break;
            case PEBoundImportAddressTable32 peBoundImportAddressTable32:
                Print(peBoundImportAddressTable32, ref writer);
                break;
            case PEBoundImportAddressTable64 peBoundImportAddressTable64:
                Print(peBoundImportAddressTable64, ref writer);
                break;
            case PEExportAddressTable peExportAddressTable:
                Print(peExportAddressTable, ref writer);
                break;
            case PEExportNameTable peExportNameTable:
                Print(peExportNameTable, ref writer);
                break;
            case PEExportOrdinalTable peExportOrdinalTable:
                Print(peExportOrdinalTable, ref writer);
                break;
            case PEImportFunctionTable peImportFunctionTable:
                Print(peImportFunctionTable, ref writer);
                break;
            case PEStreamSectionData peStreamSectionData:
                Print(peStreamSectionData, ref writer);
                break;
            case PEDebugSectionDataRSDS peDebugSectionDataRSDS:
                Print(peDebugSectionDataRSDS, ref writer);
                break;
            case PEResourceEntry peResourceEntry:
                Print(peResourceEntry, ref writer);
                break;
            default:
                writer.WriteLine($"Unsupported section data {data}");
                break;
        }

        if (data is PEDataDirectory directory && directory.Content.Count > 0)
        {
            foreach (var content in directory.Content)
            {
                PrintSectionData(file, content, ref writer);
            }
        }

        writer.Unindent();
        writer.WriteLine();
    }

    private static void Print(PEDebugSectionDataRSDS data, ref TextWriterIndenter writer)
    {
        const int indent = -26;
        writer.WriteLine("Debug Section Data (RSDS)");
        writer.Indent();
        writer.WriteLine($"{nameof(PEDebugSectionDataRSDS.Guid),indent} = {data.Guid}");
        writer.WriteLine($"{nameof(PEDebugSectionDataRSDS.Age),indent} = {data.Age}");
        writer.WriteLine($"{nameof(PEDebugSectionDataRSDS.PdbPath),indent} = {data.PdbPath}");
        writer.Unindent();
    }
    
    private static void Print(PEBaseRelocationDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEBaseRelocationBlock block, ref TextWriterIndenter writer)
    {
        var pageRVA = block.PageLink.RVA();
        writer.WriteLine($"Block {pageRVA} Relocations[{block.Relocations.Count}]");

        var peFile = block.GetPEFile()!;

        writer.Indent();
        for (var i = 0; i < block.Relocations.Count; i++)
        {
            var reloc = block.Relocations[i];
            var relocRVA = block.GetRVA(reloc);
            var offsetInPage = relocRVA - pageRVA;


            var section = block.PageLink.Container!;
            section.TryFindSectionDataByRVA(relocRVA, out var sectionData);
            

            if (reloc.Type == PEBaseRelocationType.Dir64)
            {
                writer.WriteLine($"[{i:000}] {reloc.Type} Offset = 0x{offsetInPage:X4}, RVA = {relocRVA} (0x{block.ReadAddress(peFile, reloc):X16}), SectionData = {{ {PELink(sectionData)} }}");
            }
            else if (reloc.Type == PEBaseRelocationType.Absolute)
            {
                writer.WriteLine($"[{i:000}] {reloc.Type} Zero padding");
            }
            else
            {
                writer.WriteLine($"[{i:000}] {reloc.Type} Offset = 0x{offsetInPage:X4}, RVA = {relocRVA}, SectionData = {{ {PELink(sectionData)} }}");
            }
        }

        writer.Unindent();
    }

    private static void Print(PEBoundImportDirectory data, ref TextWriterIndenter writer)
    {
        foreach (var entry in data.Entries)
        {
            writer.WriteLine($"ModuleName = {entry.ModuleName.Resolve()} ({entry.ModuleName}), ForwarderRefs[{entry.ForwarderRefs.Count}]");

            writer.Indent();
            foreach (var forwarderRef in entry.ForwarderRefs)
            {
                writer.WriteLine($"ForwarderRef = {forwarderRef.ModuleName.Resolve()} ({forwarderRef.ModuleName})");
            }
            writer.Unindent();
        }
    }

    private static void Print(PEClrMetadata data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEArchitectureDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEDebugDirectory data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            writer.WriteLine(
                $"[{i}] Type = {entry.Type}, Characteristics = 0x{entry.Characteristics:X}, Version = {entry.MajorVersion}.{entry.MinorVersion}, TimeStamp = 0x{entry.TimeDateStamp:X}, Data = {PELink((PEObjectBase?)entry.SectionData ?? entry.ExtraData)}");
        }
    }

    private static void Print(PEDelayImportDirectory data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var dirEntry = data.Entries[i];
            writer.WriteLine($"[{i}] DllName = {dirEntry.DllName.Resolve()}, RVA = {dirEntry.DllName.RVA()}");
            writer.WriteLine($"[{i}] Attributes = {dirEntry.Attributes}");
            writer.WriteLine($"[{i}] DelayImportAddressTable {PELink(dirEntry.DelayImportAddressTable)}");
            writer.WriteLine($"[{i}] DelayImportNameTable {PELink(dirEntry.DelayImportNameTable)}");
            writer.WriteLine($"[{i}] BoundImportAddressTable {dirEntry.BoundImportAddressTableLink}");
            writer.WriteLine($"[{i}] UnloadDelayInformationTable {dirEntry.UnloadDelayInformationTableLink}");
            writer.WriteLine();
        }
    }

    private static void Print(PEExceptionDirectory data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            switch (entry)
            {
                case PEExceptionFunctionEntryARM entryArm:
                    writer.WriteLine($"[{i}] Begin = {entry.BeginAddress.RVA()}");
                    writer.WriteLine($"[{i}] UnwindData = 0x{entryArm.UnwindData:X}");
                    break;
                case PEExceptionFunctionEntryX86 entryX86:
                    writer.WriteLine($"[{i}] Begin = {entry.BeginAddress}");
                    writer.WriteLine($"[{i}] End = {entryX86.EndAddress}");
                    writer.WriteLine($"[{i}] UnwindInfoAddress = {entryX86.UnwindInfoAddress}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry));
            }
            writer.WriteLine();
        }
    }

    private static void Print(PEExportDirectory data, ref TextWriterIndenter writer)
    {
        writer.WriteLine($"{nameof(PEExportDirectory.TimeStamp)} = {data.TimeStamp}");
        writer.WriteLine($"{nameof(PEExportDirectory.MajorVersion)} = {data.MajorVersion}");
        writer.WriteLine($"{nameof(PEExportDirectory.MinorVersion)} = {data.MinorVersion}");
        writer.WriteLine($"{nameof(PEExportDirectory.OrdinalBase)} = 0x{data.OrdinalBase:X}");
        writer.WriteLine($"{nameof(PEExportDirectory.NameLink)} = {data.NameLink.Resolve()} ({data.NameLink})");
        writer.WriteLine($"{nameof(PEExportDirectory.ExportFunctionAddressTable)} = {PELink(data.ExportFunctionAddressTable)}");
        writer.WriteLine($"{nameof(PEExportDirectory.ExportNameTable)} = {PELink(data.ExportNameTable)}");
        writer.WriteLine($"{nameof(PEExportDirectory.ExportOrdinalTable)} = {PELink(data.ExportOrdinalTable)}");
    }

    private static void Print(PEGlobalPointerDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEImportAddressTableDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEImportDirectory data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            writer.WriteLine($"[{i}] ImportDllNameLink = {entry.ImportDllNameLink.Resolve()} ({entry.ImportDllNameLink})");
            writer.WriteLine($"[{i}] ImportAddressTable = {PELink(entry.ImportAddressTable)}");
            writer.WriteLine($"[{i}] ImportLookupTable = {PELink(entry.ImportLookupTable)}");
            writer.WriteLine();
        }
    }

    private static void Print(PELoadConfigDirectory32 data, ref TextWriterIndenter writer)
    {
        const int indent = -32;
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.Size),indent} = 0x{data.Data.Size:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.TimeDateStamp),indent} = 0x{data.Data.TimeDateStamp:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.MajorVersion),indent} = {data.Data.MajorVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.MinorVersion),indent} = {data.Data.MinorVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GlobalFlagsClear),indent} = 0x{data.Data.GlobalFlagsClear:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GlobalFlagsSet),indent} = 0x{data.Data.GlobalFlagsSet:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.CriticalSectionDefaultTimeout),indent} = 0x{data.Data.CriticalSectionDefaultTimeout:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DeCommitFreeBlockThreshold),indent} = 0x{data.Data.DeCommitFreeBlockThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DeCommitTotalFreeThreshold),indent} = 0x{data.Data.DeCommitTotalFreeThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.LockPrefixTable),indent} = 0x{data.Data.LockPrefixTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.MaximumAllocationSize),indent} = 0x{data.Data.MaximumAllocationSize:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.VirtualMemoryThreshold),indent} = 0x{data.Data.VirtualMemoryThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.ProcessAffinityMask),indent} = 0x{data.Data.ProcessAffinityMask:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.ProcessHeapFlags),indent} = 0x{data.Data.ProcessHeapFlags:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.CSDVersion),indent} = {data.Data.CSDVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DependentLoadFlags),indent} = 0x{data.Data.DependentLoadFlags:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.EditList),indent} = {data.Data.EditList}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.SecurityCookie),indent} = {data.Data.SecurityCookie}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.SEHandlerTable),indent} = {data.Data.SEHandlerTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.SEHandlerCount),indent} = 0x{data.Data.SEHandlerCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardCFCheckFunctionPointer),indent} = {data.Data.GuardCFCheckFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardCFDispatchFunctionPointer),indent} = {data.Data.GuardCFDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardCFFunctionTable),indent} = {data.Data.GuardCFFunctionTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardCFFunctionCount),indent} = 0x{data.Data.GuardCFFunctionCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardFlags),indent} = {data.Data.GuardFlags}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.TableSizeShift),indent} = 0x{data.Data.TableSizeShift:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Flags)}",indent} = 0x{data.Data.CodeIntegrity.Flags:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Catalog)}",indent} = 0x{data.Data.CodeIntegrity.Catalog:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.CatalogOffset)}",indent} = 0x{data.Data.CodeIntegrity.CatalogOffset:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Reserved)}",indent} = 0x{data.Data.CodeIntegrity.Reserved:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardAddressTakenIatEntryTable),indent} = {data.Data.GuardAddressTakenIatEntryTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardAddressTakenIatEntryCount),indent} = 0x{data.Data.GuardAddressTakenIatEntryCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardLongJumpTargetTable),indent} = {data.Data.GuardLongJumpTargetTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardLongJumpTargetCount),indent} = 0x{data.Data.GuardLongJumpTargetCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DynamicValueRelocTable),indent} = {data.Data.DynamicValueRelocTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.CHPEMetadataPointer),indent} = {data.Data.CHPEMetadataPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardRFFailureRoutine),indent} = {data.Data.GuardRFFailureRoutine}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardRFFailureRoutineFunctionPointer),indent} = {data.Data.GuardRFFailureRoutineFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DynamicValueRelocTableOffset),indent} = 0x{data.Data.DynamicValueRelocTableOffset:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.DynamicValueRelocTableSection),indent} = {data.Data.DynamicValueRelocTableSection}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.Reserved2),indent} = {data.Data.Reserved2}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardRFVerifyStackPointerFunctionPointer),indent} = {data.Data.GuardRFVerifyStackPointerFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.HotPatchTableOffset),indent} = 0x{data.Data.HotPatchTableOffset:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.Reserved3),indent} = 0x{data.Data.Reserved3:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.EnclaveConfigurationPointer),indent} = {data.Data.EnclaveConfigurationPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.VolatileMetadataPointer),indent} = {data.Data.VolatileMetadataPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardEHContinuationTable),indent} = {data.Data.GuardEHContinuationTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardEHContinuationCount),indent} = 0x{data.Data.GuardEHContinuationCount}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardXFGCheckFunctionPointer),indent} = {data.Data.GuardXFGCheckFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardXFGDispatchFunctionPointer),indent} = {data.Data.GuardXFGDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardXFGTableDispatchFunctionPointer),indent} = {data.Data.GuardXFGTableDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.CastGuardOsDeterminedFailureMode),indent} = {data.Data.CastGuardOsDeterminedFailureMode}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData32.GuardMemcpyFunctionPointer),indent} = {data.Data.GuardMemcpyFunctionPointer}");
    }

    private static void Print(PELoadConfigDirectory64 data, ref TextWriterIndenter writer)
    {
        const int indent = -32;

        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.Size),indent} = 0x{data.Data.Size:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.TimeDateStamp),indent} = 0x{data.Data.TimeDateStamp:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.MajorVersion),indent} = {data.Data.MajorVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.MinorVersion),indent} = {data.Data.MinorVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GlobalFlagsClear),indent} = 0x{data.Data.GlobalFlagsClear:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GlobalFlagsSet),indent} = 0x{data.Data.GlobalFlagsSet:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.CriticalSectionDefaultTimeout),indent} = 0x{data.Data.CriticalSectionDefaultTimeout:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DeCommitFreeBlockThreshold),indent} = 0x{data.Data.DeCommitFreeBlockThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DeCommitTotalFreeThreshold),indent} = 0x{data.Data.DeCommitTotalFreeThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.LockPrefixTable),indent} = 0x{data.Data.LockPrefixTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.MaximumAllocationSize),indent} = 0x{data.Data.MaximumAllocationSize:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.VirtualMemoryThreshold),indent} = 0x{data.Data.VirtualMemoryThreshold:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.ProcessAffinityMask),indent} = 0x{data.Data.ProcessAffinityMask:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.ProcessHeapFlags),indent} = 0x{data.Data.ProcessHeapFlags:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.CSDVersion),indent} = {data.Data.CSDVersion}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DependentLoadFlags),indent} = 0x{data.Data.DependentLoadFlags:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.EditList),indent} = {data.Data.EditList}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.SecurityCookie),indent} = {data.Data.SecurityCookie}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.SEHandlerTable),indent} = {data.Data.SEHandlerTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.SEHandlerCount),indent} = 0x{data.Data.SEHandlerCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardCFCheckFunctionPointer),indent} = {data.Data.GuardCFCheckFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardCFDispatchFunctionPointer),indent} = {data.Data.GuardCFDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardCFFunctionTable),indent} = {data.Data.GuardCFFunctionTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardCFFunctionCount),indent} = 0x{data.Data.GuardCFFunctionCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardFlags),indent} = {data.Data.GuardFlags}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.TableSizeShift),indent} = 0x{data.Data.TableSizeShift:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Flags)}",indent} = 0x{data.Data.CodeIntegrity.Flags:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Catalog)}",indent} = 0x{data.Data.CodeIntegrity.Catalog:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.CatalogOffset)}",indent} = 0x{data.Data.CodeIntegrity.CatalogOffset:X}");
        writer.WriteLine($"{$"{nameof(PELoadConfigDirectoryData64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Reserved)}",indent} = 0x{data.Data.CodeIntegrity.Reserved:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardAddressTakenIatEntryTable),indent} = {data.Data.GuardAddressTakenIatEntryTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardAddressTakenIatEntryCount),indent} = 0x{data.Data.GuardAddressTakenIatEntryCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardLongJumpTargetTable),indent} = {data.Data.GuardLongJumpTargetTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardLongJumpTargetCount),indent} = 0x{data.Data.GuardLongJumpTargetCount:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DynamicValueRelocTable),indent} = {data.Data.DynamicValueRelocTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.CHPEMetadataPointer),indent} = {data.Data.CHPEMetadataPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardRFFailureRoutine),indent} = {data.Data.GuardRFFailureRoutine}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardRFFailureRoutineFunctionPointer),indent} = {data.Data.GuardRFFailureRoutineFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DynamicValueRelocTableOffset),indent} = 0x{data.Data.DynamicValueRelocTableOffset:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.DynamicValueRelocTableSection),indent} = {data.Data.DynamicValueRelocTableSection}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.Reserved2),indent} = {data.Data.Reserved2}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardRFVerifyStackPointerFunctionPointer),indent} = {data.Data.GuardRFVerifyStackPointerFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.HotPatchTableOffset),indent} = 0x{data.Data.HotPatchTableOffset:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.Reserved3),indent} = 0x{data.Data.Reserved3:X}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.EnclaveConfigurationPointer),indent} = {data.Data.EnclaveConfigurationPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.VolatileMetadataPointer),indent} = {data.Data.VolatileMetadataPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardEHContinuationTable),indent} = {data.Data.GuardEHContinuationTable}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardEHContinuationCount),indent} = 0x{data.Data.GuardEHContinuationCount}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardXFGCheckFunctionPointer),indent} = {data.Data.GuardXFGCheckFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardXFGDispatchFunctionPointer),indent} = {data.Data.GuardXFGDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardXFGTableDispatchFunctionPointer),indent} = {data.Data.GuardXFGTableDispatchFunctionPointer}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.CastGuardOsDeterminedFailureMode),indent} = {data.Data.CastGuardOsDeterminedFailureMode}");
        writer.WriteLine($"{nameof(PELoadConfigDirectoryData64.GuardMemcpyFunctionPointer),indent} = {data.Data.GuardMemcpyFunctionPointer}");
    }

    private static void Print(PEResourceDirectory data, ref TextWriterIndenter writer)
    {
        writer.Indent();
        Print(data.Root, ref writer);
        writer.Unindent();
    }

    private static void Print(PEResourceEntry data, ref TextWriterIndenter writer)
    {
        switch (data)
        {
            case PEResourceDataEntry resourceFile:
                writer.WriteLine($"> CodePage = {resourceFile.CodePage?.EncodingName ?? "null"}, Data = {resourceFile.Data}");
                break;
            case PEResourceDirectoryEntry dir:
                writer.WriteLine($"> ByNames[{dir.ByNames.Count}], ByIds[{dir.ByIds.Count}] , TimeDateStamp = {dir.TimeDateStamp}, Version = {dir.MajorVersion}.{dir.MinorVersion}");
                writer.Indent();

                for (var i = 0; i < dir.ByNames.Count; i++)
                {
                    var entry = dir.ByNames[i];
                    writer.WriteLine($"[{i}] Name = {entry.Name}, Entry = {entry.Entry}");
                }

                for (var i = 0; i < dir.ByIds.Count; i++)
                {
                    var entry = dir.ByIds[i];
                    writer.WriteLine($"[{i}] Id = {entry.Id}, Entry = {entry.Entry}");
                }

                writer.Unindent();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(data));
        }
    }

    private static void Print(PETlsDirectory32 data, ref TextWriterIndenter writer)
    {
        writer.WriteLine($"{nameof(PETlsDirectory32.StartAddressOfRawData)} = {data.StartAddressOfRawData}");
        writer.WriteLine($"{nameof(PETlsDirectory32.EndAddressOfRawData)} = {data.EndAddressOfRawData}");
        writer.WriteLine($"{nameof(PETlsDirectory32.AddressOfIndex)} = {data.AddressOfIndex}");
        writer.WriteLine($"{nameof(PETlsDirectory32.AddressOfCallBacks)} = {data.AddressOfCallBacks}");
        writer.WriteLine($"{nameof(PETlsDirectory32.SizeOfZeroFill)} = 0x{data.SizeOfZeroFill:X}");
        writer.WriteLine($"{nameof(PETlsDirectory32.Characteristics)} = {data.Characteristics}");
    }

    private static void Print(PETlsDirectory64 data, ref TextWriterIndenter writer)
    {
        writer.WriteLine($"{nameof(PETlsDirectory64.StartAddressOfRawData)} = {data.StartAddressOfRawData}");
        writer.WriteLine($"{nameof(PETlsDirectory64.EndAddressOfRawData)} = {data.EndAddressOfRawData}");
        writer.WriteLine($"{nameof(PETlsDirectory64.AddressOfIndex)} = {data.AddressOfIndex}");
        writer.WriteLine($"{nameof(PETlsDirectory64.AddressOfCallBacks)} = {data.AddressOfCallBacks}");
        writer.WriteLine($"{nameof(PETlsDirectory64.SizeOfZeroFill)} = 0x{data.SizeOfZeroFill:X}");
        writer.WriteLine($"{nameof(PETlsDirectory64.Characteristics)} = {data.Characteristics}");
    }

    private static void Print(PEBoundImportAddressTable32 data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            writer.WriteLine($"[{i}] VA = {entry}");
        }
    }

    private static void Print(PEBoundImportAddressTable64 data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            writer.WriteLine($"[{i}] VA = {entry}");
        }
    }

    private static void Print(PEExportAddressTable data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Values.Count; i++)
        {
            var entry = data.Values[i];
            if (entry.IsForwarderRVA)
            {
                writer.WriteLine($"[{i}] Forwarder RVA = {entry.ForwarderRVA.RVA()} ({PELink(entry.ForwarderRVA.Container)})");
            }
            else
            {
                writer.WriteLine($"[{i}] Exported RVA = {entry.ExportRVA.RVA()} ({PELink(entry.ExportRVA.Container)})");
            }
        }
    }

    private static void Print(PEExportNameTable data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Values.Count; i++)
        {
            var entry = data.Values[i];
            writer.WriteLine($"[{i}] {entry.Resolve()} ({entry})");
        }
    }

    private static void Print(PEExportOrdinalTable data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Values.Count; i++)
        {
            var entry = data.Values[i];
            writer.WriteLine($"[{i}] Ordinal = {entry}");
        }
    }

    private static void Print(PEImportFunctionTable data, ref TextWriterIndenter writer)
    {
        for (var i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            if (entry.IsImportByOrdinal)
            {
                writer.WriteLine($"[{i}] Ordinal = {entry.Ordinal}");
            }
            else
            {
                writer.WriteLine($"[{i}] {entry.HintName.Resolve()} ({entry.HintName})");
            }
        }
    }

    private static void Print(PEStreamSectionData data, ref TextWriterIndenter writer)
    {
    }

    private static string PEDescribe(PEObjectBase? peObjectBase)
    {
        const int indent = -32;
        if (peObjectBase is PEObject peObject)
        {
            return $"{peObject.GetType().Name, indent} Position = 0x{peObject.Position:X8}, Size = 0x{peObject.Size:X8}, RVA = 0x{peObject.RVA.Value:X8}, VirtualSize = 0x{peObject.VirtualSize:X8}";
        }
        else if (peObjectBase is not null)
        {
            return $"{peObjectBase.GetType().Name,indent} Position = 0x{peObjectBase.Position:X8}, Size = 0x{peObjectBase.Size:X8}";
        }
        else
        {
            return "null";
        }
    }

    private static string PELink(PEObjectBase? peObjectBase)
    {
        if (peObjectBase is PEObject peObject)
        {
            return $"RVA = 0x{peObject.RVA.Value:X8} ({peObject.GetType().Name}[{peObject.Index}]{PEParent((PEObjectBase?)peObject.Parent)})";
        }
        else if (peObjectBase is not null)
        {
            return $"({peObjectBase.GetType().Name}[{peObjectBase.Index}]{PEParent((PEObjectBase?)peObjectBase.Parent)}";
        }
        else
        {
            return "null";
        }

        static string PEParent(PEObjectBase? obj)
        {
            if (obj is PESection section)
            {
                return $" -> {section.Name}";
            }
            else if (obj is PESectionData sectionData)
            {
                return $" -> {sectionData.GetType().Name}[{sectionData.Index}]{PEParent((PEObjectBase?)sectionData.Parent)}";
            }

            if (obj is not null)
            {
                return $" -> {obj.GetType().Name}";
            }

            return "";
        }
    }
}