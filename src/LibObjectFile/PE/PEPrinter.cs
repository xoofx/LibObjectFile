// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.IO;

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
            writer.WriteLine($"{nameof(ImageDosHeader.Magic),indent} = {file.DosHeader.Magic}");
            writer.WriteLine($"{nameof(ImageDosHeader.ByteCountOnLastPage),indent} = 0x{file.DosHeader.ByteCountOnLastPage:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.PageCount),indent} = 0x{file.DosHeader.PageCount:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.RelocationCount),indent} = 0x{file.DosHeader.RelocationCount:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.SizeOfParagraphsHeader),indent} = 0x{file.DosHeader.SizeOfParagraphsHeader:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.MinExtraParagraphs),indent} = 0x{file.DosHeader.MinExtraParagraphs:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.MaxExtraParagraphs),indent} = 0x{file.DosHeader.MaxExtraParagraphs:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.InitialSSValue),indent} = 0x{file.DosHeader.InitialSSValue:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.InitialSPValue),indent} = 0x{file.DosHeader.InitialSPValue:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.Checksum),indent} = 0x{file.DosHeader.Checksum:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.InitialIPValue),indent} = 0x{file.DosHeader.InitialIPValue:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.InitialCSValue),indent} = 0x{file.DosHeader.InitialCSValue:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.FileAddressRelocationTable),indent} = 0x{file.DosHeader.FileAddressRelocationTable:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.OverlayNumber),indent} = 0x{file.DosHeader.OverlayNumber:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.Reserved),indent} = 0x{file.DosHeader.Reserved[0]:X}, 0x{file.DosHeader.Reserved[1]:X}, 0x{file.DosHeader.Reserved[2]:X}, 0x{file.DosHeader.Reserved[3]:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.OEMIdentifier),indent} = 0x{file.DosHeader.OEMIdentifier:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.OEMInformation),indent} = 0x{file.DosHeader.OEMInformation:X}");
            writer.WriteLine(
                $"{nameof(ImageDosHeader.Reserved2),indent} = 0x{file.DosHeader.Reserved2[0]:X}, 0x{file.DosHeader.Reserved2[1]:X}, 0x{file.DosHeader.Reserved2[2]:X}, 0x{file.DosHeader.Reserved2[3]:X}, 0x{file.DosHeader.Reserved2[4]:X}, 0x{file.DosHeader.Reserved2[5]:X}, 0x{file.DosHeader.Reserved2[6]:X}, 0x{file.DosHeader.Reserved2[7]:X}, 0x{file.DosHeader.Reserved2[8]:X}, 0x{file.DosHeader.Reserved2[9]:X}");
            writer.WriteLine($"{nameof(ImageDosHeader.FileAddressPEHeader),indent} = 0x{file.DosHeader.FileAddressPEHeader:X}");
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
            writer.WriteLine($"{nameof(ImageCoffHeader.Machine),indent} = {file.CoffHeader.Machine}");
            writer.WriteLine($"{nameof(ImageCoffHeader.NumberOfSections),indent} = {file.CoffHeader.NumberOfSections}");
            writer.WriteLine($"{nameof(ImageCoffHeader.TimeDateStamp),indent} = {file.CoffHeader.TimeDateStamp}");
            writer.WriteLine($"{nameof(ImageCoffHeader.PointerToSymbolTable),indent} = 0x{file.CoffHeader.PointerToSymbolTable:X}");
            writer.WriteLine($"{nameof(ImageCoffHeader.NumberOfSymbols),indent} = {file.CoffHeader.NumberOfSymbols}");
            writer.WriteLine($"{nameof(ImageCoffHeader.SizeOfOptionalHeader),indent} = {file.CoffHeader.SizeOfOptionalHeader}");
            writer.WriteLine($"{nameof(ImageCoffHeader.Characteristics),indent} = {file.CoffHeader.Characteristics}");
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
            writer.WriteLine($"{nameof(ImageOptionalHeader.Magic),indent} = {file.OptionalHeader.Magic}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MajorLinkerVersion),indent} = {file.OptionalHeader.MajorLinkerVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MinorLinkerVersion),indent} = {file.OptionalHeader.MinorLinkerVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfCode),indent} = 0x{file.OptionalHeader.SizeOfCode:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfInitializedData),indent} = 0x{file.OptionalHeader.SizeOfInitializedData:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfUninitializedData),indent} = 0x{file.OptionalHeader.SizeOfUninitializedData:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.AddressOfEntryPoint),indent} = 0x{file.OptionalHeader.AddressOfEntryPoint:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.BaseOfCode),indent} = 0x{file.OptionalHeader.BaseOfCode:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.BaseOfData),indent} = 0x{file.OptionalHeader.BaseOfData:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.ImageBase),indent} = 0x{file.OptionalHeader.ImageBase:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SectionAlignment),indent} = 0x{file.OptionalHeader.SectionAlignment:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.FileAlignment),indent} = 0x{file.OptionalHeader.FileAlignment:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MajorOperatingSystemVersion),indent} = {file.OptionalHeader.MajorOperatingSystemVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MinorOperatingSystemVersion),indent} = {file.OptionalHeader.MinorOperatingSystemVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MajorImageVersion),indent} = {file.OptionalHeader.MajorImageVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MinorImageVersion),indent} = {file.OptionalHeader.MinorImageVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MajorSubsystemVersion),indent} = {file.OptionalHeader.MajorSubsystemVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.MinorSubsystemVersion),indent} = {file.OptionalHeader.MinorSubsystemVersion}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.Win32VersionValue),indent} = 0x{file.OptionalHeader.Win32VersionValue:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfImage),indent} = 0x{file.OptionalHeader.SizeOfImage:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfHeaders),indent} = 0x{file.OptionalHeader.SizeOfHeaders:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.CheckSum),indent} = 0x{file.OptionalHeader.CheckSum:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.Subsystem),indent} = {file.OptionalHeader.Subsystem}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.DllCharacteristics),indent} = {file.OptionalHeader.DllCharacteristics}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfStackReserve),indent} = 0x{file.OptionalHeader.SizeOfStackReserve:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfStackCommit),indent} = 0x{file.OptionalHeader.SizeOfStackCommit:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfHeapReserve),indent} = 0x{file.OptionalHeader.SizeOfHeapReserve:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.SizeOfHeapCommit),indent} = 0x{file.OptionalHeader.SizeOfHeapCommit:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.LoaderFlags),indent} = 0x{file.OptionalHeader.LoaderFlags:X}");
            writer.WriteLine($"{nameof(ImageOptionalHeader.NumberOfRvaAndSizes),indent} = 0x{file.OptionalHeader.NumberOfRvaAndSizes:X}");
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void PrintDataDirectories(PEFile file, ref TextWriterIndenter writer)
    {
        writer.WriteLine("Data Directories");
        writer.Indent();
        for(int i = (int)PEDataDirectoryKind.Export; i <= (int)PEDataDirectoryKind.ClrMetadata; i++)
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
        writer.WriteLine("Sections");
        for (var i = 0; i < file.Sections.Count; i++)
        {
            var section = file.Sections[i];
            writer.Indent();
            writer.WriteLine($"[{i:00}] {section.Name,8} {PEDescribe(section)}, Characteristics = 0x{(uint)section.Characteristics:X8} ({section.Characteristics})");
            writer.WriteLine();
            foreach (var data in section.Content)
            {
                writer.Indent();
                PrintSectionData(file, data, ref writer);
                writer.Unindent();
            }

            writer.Unindent();
            writer.WriteLine();
        }
    }
    
    private static void PrintSectionData(PEFile file, PESectionData data, ref TextWriterIndenter writer)
    {
        writer.WriteLine($"Section Data [{data.Index:00}] {PEDescribe(data)}");
        writer.Indent();
        switch (data)
        {
            case PEBaseRelocationDirectory peBaseRelocationDirectory:
                Print(file, peBaseRelocationDirectory, ref writer);
                break;
            case PEBoundImportDirectory peBoundImportDirectory:
                Print(file, peBoundImportDirectory, ref writer);
                break;
            case PEClrMetadata peClrMetadata:
                Print(file, peClrMetadata, ref writer);
                break;
            case PEArchitectureDirectory peArchitectureDirectory:
                Print(file, peArchitectureDirectory, ref writer);
                break;
            case PEDebugDirectory peDebugDirectory:
                Print(file, peDebugDirectory, ref writer);
                break;
            case PEDelayImportDirectory peDelayImportDirectory:
                Print(file, peDelayImportDirectory, ref writer);
                break;
            case PEExceptionDirectory peExceptionDirectory:
                Print(file, peExceptionDirectory, ref writer);
                break;
            case PEExportDirectory peExportDirectory:
                Print(file, peExportDirectory, ref writer);
                break;
            case PEGlobalPointerDirectory peGlobalPointerDirectory:
                Print(file, peGlobalPointerDirectory, ref writer);
                break;
            case PEImportAddressTableDirectory peImportAddressTableDirectory:
                Print(file, peImportAddressTableDirectory, ref writer);
                break;
            case PEImportDirectory peImportDirectory:
                Print(file, peImportDirectory, ref writer);
                break;
            case PELoadConfigDirectory32 peLoadConfigDirectory:
                Print(file, peLoadConfigDirectory, ref writer);
                break;
            case PELoadConfigDirectory64 peLoadConfigDirectory:
                Print(file, peLoadConfigDirectory, ref writer);
                break;
            case PEResourceDirectory peResourceDirectory:
                Print(file, peResourceDirectory, ref writer);
                break;
            case PETlsDirectory32 peTlsDirectory32:
                Print(file, peTlsDirectory32, ref writer);
                break;
            case PETlsDirectory64 peTlsDirectory64:
                Print(file, peTlsDirectory64, ref writer);
                break;
            case PEDataDirectory peDataDirectory:
                Print(file, peDataDirectory, ref writer);
                break;
            case PEBoundImportAddressTable32 peBoundImportAddressTable32:
                Print(file, peBoundImportAddressTable32, ref writer);
                break;
            case PEBoundImportAddressTable64 peBoundImportAddressTable64:
                Print(file, peBoundImportAddressTable64, ref writer);
                break;
            case PEDelayImportAddressTable peDelayImportAddressTable:
                Print(file, peDelayImportAddressTable, ref writer);
                break;
            case PEExportAddressTable peExportAddressTable:
                Print(file, peExportAddressTable, ref writer);
                break;
            case PEExportNameTable peExportNameTable:
                Print(file, peExportNameTable, ref writer);
                break;
            case PEExportOrdinalTable peExportOrdinalTable:
                Print(file, peExportOrdinalTable, ref writer);
                break;
            case PEImportAddressTable peImportAddressTable:
                Print(file, peImportAddressTable, ref writer);
                break;
            case PEImportLookupTable peImportLookupTable:
                Print(file, peImportLookupTable, ref writer);
                break;
            case PEStreamSectionData peStreamSectionData:
                Print(file, peStreamSectionData, ref writer);
                break;
            case PEDebugSectionDataRSDS peDebugSectionDataRSDS:
                Print(file, peDebugSectionDataRSDS, ref writer);
                break;
            default:
                writer.WriteLine($"Unsupported section data {data}");
                break;
        }
        writer.Unindent();
        writer.WriteLine();
    }

    private static void Print(PEFile file, PEDebugSectionDataRSDS data, ref TextWriterIndenter writer)
    {

    }
    
    private static void Print(PEFile file, PEBaseRelocationDirectory data, ref TextWriterIndenter writer)
    {
        foreach (var block in data.Blocks)
        {
            var pageRVA = block.SectionLink.RVA();
            writer.WriteLine($"Block {pageRVA} Relocations[{block.Relocations.Count}]");

            writer.Indent();
            foreach (var reloc in block.Relocations)
            {
                var relocRVA = reloc.RVA();
                var offsetInPage = relocRVA - pageRVA;

                if (reloc.Type == PEBaseRelocationType.Dir64)
                {
                    writer.WriteLine($"{reloc.Type,6} Offset = 0x{offsetInPage:X4}, RVA = {relocRVA} (0x{reloc.ReadAddress(file):X16}), SectionData = {{ {PEDescribe(reloc.Container)} }}");
                }
                else
                {
                    writer.WriteLine($"{reloc.Type,6} Offset = 0x{offsetInPage:X4}, RVA = {relocRVA}, SectionData = {{ {PEDescribe(reloc.Container)} }}");
                }
            }
            writer.Unindent();
        }
    }

    private static void Print(PEFile file, PEBoundImportDirectory data, ref TextWriterIndenter writer)
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

    private static void Print(PEFile file, PEClrMetadata data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEArchitectureDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEDebugDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEDelayImportDirectory data, ref TextWriterIndenter writer)
    {
        foreach (var dirEntry in data.Entries)
        {
            writer.WriteLine($"DllName = {dirEntry.DllName.Resolve()}, RVA = {dirEntry.DllName.RVA()}");
            writer.WriteLine($"Attributes = {dirEntry.Attributes}");
            writer.WriteLine($"DelayImportAddressTable RVA = {dirEntry.DelayImportAddressTable.RVA}");
            writer.WriteLine($"DelayImportNameTable RVA = {dirEntry.DelayImportNameTable.RVA}");
            writer.WriteLine($"BoundImportAddressTable RVA = {(dirEntry.BoundImportAddressTable?.RVA ?? (RVA)0)}");
            writer.WriteLine($"UnloadDelayInformationTable RVA = {(dirEntry.UnloadDelayInformationTable?.RVA ?? (RVA)0)}");
        }
    }

    private static void Print(PEFile file, PEExceptionDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEExportDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEGlobalPointerDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEImportAddressTableDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEImportDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PELoadConfigDirectory32 data, ref TextWriterIndenter writer)
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

    private static void Print(PEFile file, PELoadConfigDirectory64 data, ref TextWriterIndenter writer)
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

    private static void Print(PEFile file, PEResourceDirectory data, ref TextWriterIndenter writer)
    {

    }

    private static void Print(PEFile file, PETlsDirectory32 data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PETlsDirectory64 data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEDataDirectory data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEBoundImportAddressTable32 data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEBoundImportAddressTable64 data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEDelayImportAddressTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEExportAddressTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEExportNameTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEExportOrdinalTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEImportAddressTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEImportLookupTable data, ref TextWriterIndenter writer)
    {
    }

    private static void Print(PEFile file, PEStreamSectionData data, ref TextWriterIndenter writer)
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
}