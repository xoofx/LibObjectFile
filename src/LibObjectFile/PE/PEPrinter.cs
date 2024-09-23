// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.PE;

public static class PEPrinter
{
    public static void Print(this PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        PrintHeaders(file, writer);
        PrintDataDirectories(file, writer);
        PrintSections(file, writer);
    }

    public static void PrintHeaders(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        PrintDosHeader(file, writer);
        PrintDosStub(file, writer);
        PrintCoffHeader(file, writer);
        PrintOptionalHeader(file, writer);
    }

    public static unsafe void PrintDosHeader(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        const int indent = -26;
        writer.WriteLine("DOS Header");
        writer.WriteLine($"  {nameof(ImageDosHeader.Magic),indent} = {file.DosHeader.Magic}");
        writer.WriteLine($"  {nameof(ImageDosHeader.ByteCountOnLastPage),indent} = 0x{file.DosHeader.ByteCountOnLastPage:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.PageCount),indent} = 0x{file.DosHeader.PageCount:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.RelocationCount),indent} = 0x{file.DosHeader.RelocationCount:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.SizeOfParagraphsHeader),indent} = 0x{file.DosHeader.SizeOfParagraphsHeader:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.MinExtraParagraphs),indent} = 0x{file.DosHeader.MinExtraParagraphs:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.MaxExtraParagraphs),indent} = 0x{file.DosHeader.MaxExtraParagraphs:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.InitialSSValue),indent} = 0x{file.DosHeader.InitialSSValue:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.InitialSPValue),indent} = 0x{file.DosHeader.InitialSPValue:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.Checksum),indent} = 0x{file.DosHeader.Checksum:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.InitialIPValue),indent} = 0x{file.DosHeader.InitialIPValue:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.InitialCSValue),indent} = 0x{file.DosHeader.InitialCSValue:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.FileAddressRelocationTable),indent} = 0x{file.DosHeader.FileAddressRelocationTable:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.OverlayNumber),indent} = 0x{file.DosHeader.OverlayNumber:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.Reserved),indent} = 0x{file.DosHeader.Reserved[0]:X}, 0x{file.DosHeader.Reserved[1]:X}, 0x{file.DosHeader.Reserved[2]:X}, 0x{file.DosHeader.Reserved[3]:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.OEMIdentifier),indent} = 0x{file.DosHeader.OEMIdentifier:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.OEMInformation),indent} = 0x{file.DosHeader.OEMInformation:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.Reserved2),indent} = 0x{file.DosHeader.Reserved2[0]:X}, 0x{file.DosHeader.Reserved2[1]:X}, 0x{file.DosHeader.Reserved2[2]:X}, 0x{file.DosHeader.Reserved2[3]:X}, 0x{file.DosHeader.Reserved2[4]:X}, 0x{file.DosHeader.Reserved2[5]:X}, 0x{file.DosHeader.Reserved2[6]:X}, 0x{file.DosHeader.Reserved2[7]:X}, 0x{file.DosHeader.Reserved2[8]:X}, 0x{file.DosHeader.Reserved2[9]:X}");
        writer.WriteLine($"  {nameof(ImageDosHeader.FileAddressPEHeader),indent} = 0x{file.DosHeader.FileAddressPEHeader:X}");
        writer.WriteLine();
    }

    public static void PrintDosStub(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);
        const int indent = -26;
        writer.WriteLine("DOS Stub");
        writer.WriteLine($"  {nameof(file.DosStub),indent} = {file.DosStub.Length} bytes");
        writer.WriteLine();
    }

    public static void PrintCoffHeader(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        const int indent = -26;

        writer.WriteLine("COFF Header");
        writer.WriteLine($"  {nameof(ImageCoffHeader.Machine),indent} = {file.CoffHeader.Machine}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.NumberOfSections),indent} = {file.CoffHeader.NumberOfSections}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.TimeDateStamp),indent} = {file.CoffHeader.TimeDateStamp}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.PointerToSymbolTable),indent} = 0x{file.CoffHeader.PointerToSymbolTable:X}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.NumberOfSymbols),indent} = {file.CoffHeader.NumberOfSymbols}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.SizeOfOptionalHeader),indent} = {file.CoffHeader.SizeOfOptionalHeader}");
        writer.WriteLine($"  {nameof(ImageCoffHeader.Characteristics),indent} = {file.CoffHeader.Characteristics}");
        writer.WriteLine();
    }

    public static void PrintOptionalHeader(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        const int indent = -26;
        writer.WriteLine("Optional Header");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.Magic),indent} = {file.OptionalHeader.Magic}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MajorLinkerVersion),indent} = {file.OptionalHeader.MajorLinkerVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MinorLinkerVersion),indent} = {file.OptionalHeader.MinorLinkerVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfCode),indent} = 0x{file.OptionalHeader.SizeOfCode:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfInitializedData),indent} = 0x{file.OptionalHeader.SizeOfInitializedData:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfUninitializedData),indent} = 0x{file.OptionalHeader.SizeOfUninitializedData:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.AddressOfEntryPoint),indent} = 0x{file.OptionalHeader.AddressOfEntryPoint:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.BaseOfCode),indent} = 0x{file.OptionalHeader.BaseOfCode:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.BaseOfData),indent} = 0x{file.OptionalHeader.BaseOfData:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.ImageBase),indent} = 0x{file.OptionalHeader.ImageBase:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SectionAlignment),indent} = 0x{file.OptionalHeader.SectionAlignment:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.FileAlignment),indent} = 0x{file.OptionalHeader.FileAlignment:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MajorOperatingSystemVersion),indent} = {file.OptionalHeader.MajorOperatingSystemVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MinorOperatingSystemVersion),indent} = {file.OptionalHeader.MinorOperatingSystemVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MajorImageVersion),indent} = {file.OptionalHeader.MajorImageVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MinorImageVersion),indent} = {file.OptionalHeader.MinorImageVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MajorSubsystemVersion),indent} = {file.OptionalHeader.MajorSubsystemVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.MinorSubsystemVersion),indent} = {file.OptionalHeader.MinorSubsystemVersion}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.Win32VersionValue),indent} = 0x{file.OptionalHeader.Win32VersionValue:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfImage),indent} = 0x{file.OptionalHeader.SizeOfImage:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfHeaders),indent} = 0x{file.OptionalHeader.SizeOfHeaders:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.CheckSum),indent} = 0x{file.OptionalHeader.CheckSum:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.Subsystem),indent} = {file.OptionalHeader.Subsystem}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.DllCharacteristics),indent} = {file.OptionalHeader.DllCharacteristics}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfStackReserve),indent} = 0x{file.OptionalHeader.SizeOfStackReserve:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfStackCommit),indent} = 0x{file.OptionalHeader.SizeOfStackCommit:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfHeapReserve),indent} = 0x{file.OptionalHeader.SizeOfHeapReserve:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.SizeOfHeapCommit),indent} = 0x{file.OptionalHeader.SizeOfHeapCommit:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.LoaderFlags),indent} = 0x{file.OptionalHeader.LoaderFlags:X}");
        writer.WriteLine($"  {nameof(ImageOptionalHeader.NumberOfRvaAndSizes),indent} = 0x{file.OptionalHeader.NumberOfRvaAndSizes:X}");
        writer.WriteLine();
    }

    public static void PrintDataDirectories(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Data Directories");
        for(int i = (int)PEDataDirectoryKind.Export; i <= (int)PEDataDirectoryKind.ClrMetadata; i++)
        {
            var kind = (PEDataDirectoryKind)i;
            var directory = file.Directories[kind];
            writer.WriteLine(directory is null 
                ? $"  [{i,3}] = null" 
                : $"  [{i,3}] = {PEDescribe(directory)}");
        }
        writer.WriteLine();
    }

    public static void PrintSections(PEFile file, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Sections");
        foreach (var section in file.Sections)
        {
            writer.WriteLine($" {section.Name,8} {PEDescribe(section)}");
            writer.WriteLine();
            foreach (var data in section.Content)
            {
                PrintSectionData(file, data, writer);
            }
            writer.WriteLine();
        }
    }
    
    public static void PrintSection(PEFile file, PESection section, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"  {section.Name,-8} {PEDescribe(section)}");
        foreach (var data in section.Content)
        {
            PrintSectionData(file, data, writer);
        }
    }

    public static void PrintSectionData(PEFile file, PESectionData data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"    [{data.Index,3}] {PEDescribe(data)}");
        switch (data)
        {
            case PEBaseRelocationDirectory peBaseRelocationDirectory:
                Print(file, peBaseRelocationDirectory, writer);
                break;
            case PEBoundImportDirectory peBoundImportDirectory:
                Print(file, peBoundImportDirectory, writer);
                break;
            case PEClrMetadata peClrMetadata:
                Print(file, peClrMetadata, writer);
                break;
            case PEArchitectureDirectory peArchitectureDirectory:
                Print(file, peArchitectureDirectory, writer);
                break;
            case PEDebugDirectory peDebugDirectory:
                Print(file, peDebugDirectory, writer);
                break;
            case PEDelayImportDirectory peDelayImportDirectory:
                Print(file, peDelayImportDirectory, writer);
                break;
            case PEExceptionDirectory peExceptionDirectory:
                Print(file, peExceptionDirectory, writer);
                break;
            case PEExportDirectory peExportDirectory:
                Print(file, peExportDirectory, writer);
                break;
            case PEGlobalPointerDirectory peGlobalPointerDirectory:
                Print(file, peGlobalPointerDirectory, writer);
                break;
            case PEImportAddressTableDirectory peImportAddressTableDirectory:
                Print(file, peImportAddressTableDirectory, writer);
                break;
            case PEImportDirectory peImportDirectory:
                Print(file, peImportDirectory, writer);
                break;
            case PELoadConfigDirectory peLoadConfigDirectory:
                Print(file, peLoadConfigDirectory, writer);
                break;
            case PEResourceDirectory peResourceDirectory:
                Print(file, peResourceDirectory, writer);
                break;
            case PETlsDirectory peTlsDirectory:
                Print(file, peTlsDirectory, writer);
                break;
            case PEDataDirectory peDataDirectory:
                Print(file, peDataDirectory, writer);
                break;
            case PEBoundImportAddressTable32 peBoundImportAddressTable32:
                Print(file, peBoundImportAddressTable32, writer);
                break;
            case PEBoundImportAddressTable64 peBoundImportAddressTable64:
                Print(file, peBoundImportAddressTable64, writer);
                break;
            case PEDelayImportAddressTable peDelayImportAddressTable:
                Print(file, peDelayImportAddressTable, writer);
                break;
            case PEExportAddressTable peExportAddressTable:
                Print(file, peExportAddressTable, writer);
                break;
            case PEExportNameTable peExportNameTable:
                Print(file, peExportNameTable, writer);
                break;
            case PEExportOrdinalTable peExportOrdinalTable:
                Print(file, peExportOrdinalTable, writer);
                break;
            case PEImportAddressTable peImportAddressTable:
                Print(file, peImportAddressTable, writer);
                break;
            case PEImportLookupTable peImportLookupTable:
                Print(file, peImportLookupTable, writer);
                break;
            case PEStreamSectionData peStreamSectionData:
                Print(file, peStreamSectionData, writer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(data));
        }
        writer.WriteLine();
    }

    private static void Print(PEFile file, PEBaseRelocationDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var entry in data.Blocks)
        {
            var pageRVA = entry.SectionLink.RVA();
            writer.WriteLine($"            Block {pageRVA} Parts[{entry.Parts.Count}]");

            foreach (var part in entry.Parts)
            {
                writer.WriteLine($"              {PEDescribe(part.SectionDataLink.Container)} Relocs[{part.Relocations.Count}]");

                foreach (var reloc in part.Relocations)
                {
                    var relocRVA = part.GetRVA(reloc);
                    var offsetInPage = relocRVA - pageRVA;

                    writer.WriteLine($"                {reloc.Type,6} OffsetFromSectionData = 0x{reloc.OffsetInBlockPart:X4}, OffsetFromBlock = 0x{offsetInPage:X4}, RVA = {relocRVA}");
                }
            }
        }
    }

    private static void Print(PEFile file, PEBoundImportDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var entry in data.Entries)
        {
            writer.WriteLine($"            ModuleName = {entry.ModuleName.Resolve()} ({entry.ModuleName}), ForwarderRefs[{entry.ForwarderRefs.Count}]");

            foreach (var forwarderRef in entry.ForwarderRefs)
            {
                writer.WriteLine($"              ForwarderRef = {forwarderRef.ModuleName.Resolve()} ({forwarderRef.ModuleName})");
            }
        }
    }

    private static void Print(PEFile file, PEClrMetadata data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);
        
    }

    private static void Print(PEFile file, PEArchitectureDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEDebugDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEDelayImportDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var dirEntry in data.Entries)
        {
            writer.WriteLine($"            DllName = {dirEntry.DllName.Resolve()}, RVA = {dirEntry.DllName.RVA()}");
            writer.WriteLine($"            Attributes = {dirEntry.Attributes}");
            writer.WriteLine($"            DelayImportAddressTable RVA = {dirEntry.DelayImportAddressTable.RVA}");
            writer.WriteLine($"            DelayImportNameTable RVA = {dirEntry.DelayImportNameTable.RVA}");
            writer.WriteLine($"            BoundImportAddressTable RVA = {(dirEntry.BoundImportAddressTable?.RVA ?? (RVA)0)}");
            writer.WriteLine($"            UnloadDelayInformationTable RVA = {(dirEntry.UnloadDelayInformationTable?.RVA ?? (RVA)0)}");
        }
    }

    private static void Print(PEFile file, PEExceptionDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEExportDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEGlobalPointerDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEImportAddressTableDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEImportDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PELoadConfigDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        const int indent = -32;
        if (data.Is32Bits)
        {
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.Size),indent} = 0x{data.LoadConfigDirectory32.Size:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.TimeDateStamp),indent} = 0x{data.LoadConfigDirectory32.TimeDateStamp:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.MajorVersion),indent} = {data.LoadConfigDirectory32.MajorVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.MinorVersion),indent} = {data.LoadConfigDirectory32.MinorVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GlobalFlagsClear),indent} = 0x{data.LoadConfigDirectory32.GlobalFlagsClear:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GlobalFlagsSet),indent} = 0x{data.LoadConfigDirectory32.GlobalFlagsSet:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.CriticalSectionDefaultTimeout),indent} = 0x{data.LoadConfigDirectory32.CriticalSectionDefaultTimeout:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DeCommitFreeBlockThreshold),indent} = 0x{data.LoadConfigDirectory32.DeCommitFreeBlockThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DeCommitTotalFreeThreshold),indent} = 0x{data.LoadConfigDirectory32.DeCommitTotalFreeThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.LockPrefixTable),indent} = 0x{data.LoadConfigDirectory32.LockPrefixTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.MaximumAllocationSize),indent} = 0x{data.LoadConfigDirectory32.MaximumAllocationSize:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.VirtualMemoryThreshold),indent} = 0x{data.LoadConfigDirectory32.VirtualMemoryThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.ProcessAffinityMask),indent} = 0x{data.LoadConfigDirectory32.ProcessAffinityMask:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.ProcessHeapFlags),indent} = 0x{data.LoadConfigDirectory32.ProcessHeapFlags:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.CSDVersion),indent} = {data.LoadConfigDirectory32.CSDVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DependentLoadFlags),indent} = 0x{data.LoadConfigDirectory32.DependentLoadFlags:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.EditList),indent} = {data.LoadConfigDirectory32.EditList}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.SecurityCookie),indent} = {data.LoadConfigDirectory32.SecurityCookie}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.SEHandlerTable),indent} = {data.LoadConfigDirectory32.SEHandlerTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.SEHandlerCount),indent} = 0x{data.LoadConfigDirectory32.SEHandlerCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardCFCheckFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardCFCheckFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardCFDispatchFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardCFDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardCFFunctionTable),indent} = {data.LoadConfigDirectory32.GuardCFFunctionTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardCFFunctionCount),indent} = 0x{data.LoadConfigDirectory32.GuardCFFunctionCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardFlags),indent} = {data.LoadConfigDirectory32.GuardFlags}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.TableSizeShift),indent} = 0x{data.LoadConfigDirectory32.TableSizeShift:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Flags)}",indent} = 0x{data.LoadConfigDirectory32.CodeIntegrity.Flags:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Catalog)}",indent} = 0x{data.LoadConfigDirectory32.CodeIntegrity.Catalog:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.CatalogOffset)}",indent} = 0x{data.LoadConfigDirectory32.CodeIntegrity.CatalogOffset:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory32.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Reserved)}",indent} = 0x{data.LoadConfigDirectory32.CodeIntegrity.Reserved:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardAddressTakenIatEntryTable),indent} = {data.LoadConfigDirectory32.GuardAddressTakenIatEntryTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardAddressTakenIatEntryCount),indent} = 0x{data.LoadConfigDirectory32.GuardAddressTakenIatEntryCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardLongJumpTargetTable),indent} = {data.LoadConfigDirectory32.GuardLongJumpTargetTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardLongJumpTargetCount),indent} = 0x{data.LoadConfigDirectory32.GuardLongJumpTargetCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DynamicValueRelocTable),indent} = {data.LoadConfigDirectory32.DynamicValueRelocTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.CHPEMetadataPointer),indent} = {data.LoadConfigDirectory32.CHPEMetadataPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardRFFailureRoutine),indent} = {data.LoadConfigDirectory32.GuardRFFailureRoutine}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardRFFailureRoutineFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardRFFailureRoutineFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DynamicValueRelocTableOffset),indent} = 0x{data.LoadConfigDirectory32.DynamicValueRelocTableOffset:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.DynamicValueRelocTableSection),indent} = {data.LoadConfigDirectory32.DynamicValueRelocTableSection}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.Reserved2),indent} = {data.LoadConfigDirectory32.Reserved2}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardRFVerifyStackPointerFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardRFVerifyStackPointerFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.HotPatchTableOffset),indent} = 0x{data.LoadConfigDirectory32.HotPatchTableOffset:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.Reserved3),indent} = 0x{data.LoadConfigDirectory32.Reserved3:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.EnclaveConfigurationPointer),indent} = {data.LoadConfigDirectory32.EnclaveConfigurationPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.VolatileMetadataPointer),indent} = {data.LoadConfigDirectory32.VolatileMetadataPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardEHContinuationTable),indent} = {data.LoadConfigDirectory32.GuardEHContinuationTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardEHContinuationCount),indent} = 0x{data.LoadConfigDirectory32.GuardEHContinuationCount}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardXFGCheckFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardXFGCheckFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardXFGDispatchFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardXFGDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardXFGTableDispatchFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardXFGTableDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.CastGuardOsDeterminedFailureMode),indent} = {data.LoadConfigDirectory32.CastGuardOsDeterminedFailureMode}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory32.GuardMemcpyFunctionPointer),indent} = {data.LoadConfigDirectory32.GuardMemcpyFunctionPointer}");
        }
        else
        {
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.Size),indent} = 0x{data.LoadConfigDirectory64.Size:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.TimeDateStamp),indent} = 0x{data.LoadConfigDirectory64.TimeDateStamp:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.MajorVersion),indent} = {data.LoadConfigDirectory64.MajorVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.MinorVersion),indent} = {data.LoadConfigDirectory64.MinorVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GlobalFlagsClear),indent} = 0x{data.LoadConfigDirectory64.GlobalFlagsClear:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GlobalFlagsSet),indent} = 0x{data.LoadConfigDirectory64.GlobalFlagsSet:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.CriticalSectionDefaultTimeout),indent} = 0x{data.LoadConfigDirectory64.CriticalSectionDefaultTimeout:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DeCommitFreeBlockThreshold),indent} = 0x{data.LoadConfigDirectory64.DeCommitFreeBlockThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DeCommitTotalFreeThreshold),indent} = 0x{data.LoadConfigDirectory64.DeCommitTotalFreeThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.LockPrefixTable),indent} = 0x{data.LoadConfigDirectory64.LockPrefixTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.MaximumAllocationSize),indent} = 0x{data.LoadConfigDirectory64.MaximumAllocationSize:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.VirtualMemoryThreshold),indent} = 0x{data.LoadConfigDirectory64.VirtualMemoryThreshold:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.ProcessAffinityMask),indent} = 0x{data.LoadConfigDirectory64.ProcessAffinityMask:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.ProcessHeapFlags),indent} = 0x{data.LoadConfigDirectory64.ProcessHeapFlags:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.CSDVersion),indent} = {data.LoadConfigDirectory64.CSDVersion}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DependentLoadFlags),indent} = 0x{data.LoadConfigDirectory64.DependentLoadFlags:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.EditList),indent} = {data.LoadConfigDirectory64.EditList}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.SecurityCookie),indent} = {data.LoadConfigDirectory64.SecurityCookie}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.SEHandlerTable),indent} = {data.LoadConfigDirectory64.SEHandlerTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.SEHandlerCount),indent} = 0x{data.LoadConfigDirectory64.SEHandlerCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardCFCheckFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardCFCheckFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardCFDispatchFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardCFDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardCFFunctionTable),indent} = {data.LoadConfigDirectory64.GuardCFFunctionTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardCFFunctionCount),indent} = 0x{data.LoadConfigDirectory64.GuardCFFunctionCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardFlags),indent} = {data.LoadConfigDirectory64.GuardFlags}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.TableSizeShift),indent} = 0x{data.LoadConfigDirectory64.TableSizeShift:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Flags)}",indent} = 0x{data.LoadConfigDirectory64.CodeIntegrity.Flags:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Catalog)}",indent} = 0x{data.LoadConfigDirectory64.CodeIntegrity.Catalog:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.CatalogOffset)}",indent} = 0x{data.LoadConfigDirectory64.CodeIntegrity.CatalogOffset:X}");
            writer.WriteLine($"            {$"{nameof(PELoadConfigDirectory64.CodeIntegrity)}.{nameof(PELoadConfigCodeIntegrity.Reserved)}",indent} = 0x{data.LoadConfigDirectory64.CodeIntegrity.Reserved:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardAddressTakenIatEntryTable),indent} = {data.LoadConfigDirectory64.GuardAddressTakenIatEntryTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardAddressTakenIatEntryCount),indent} = 0x{data.LoadConfigDirectory64.GuardAddressTakenIatEntryCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardLongJumpTargetTable),indent} = {data.LoadConfigDirectory64.GuardLongJumpTargetTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardLongJumpTargetCount),indent} = 0x{data.LoadConfigDirectory64.GuardLongJumpTargetCount:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DynamicValueRelocTable),indent} = {data.LoadConfigDirectory64.DynamicValueRelocTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.CHPEMetadataPointer),indent} = {data.LoadConfigDirectory64.CHPEMetadataPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardRFFailureRoutine),indent} = {data.LoadConfigDirectory64.GuardRFFailureRoutine}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardRFFailureRoutineFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardRFFailureRoutineFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DynamicValueRelocTableOffset),indent} = 0x{data.LoadConfigDirectory64.DynamicValueRelocTableOffset:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.DynamicValueRelocTableSection),indent} = {data.LoadConfigDirectory64.DynamicValueRelocTableSection}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.Reserved2),indent} = {data.LoadConfigDirectory64.Reserved2}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardRFVerifyStackPointerFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardRFVerifyStackPointerFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.HotPatchTableOffset),indent} = 0x{data.LoadConfigDirectory64.HotPatchTableOffset:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.Reserved3),indent} = 0x{data.LoadConfigDirectory64.Reserved3:X}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.EnclaveConfigurationPointer),indent} = {data.LoadConfigDirectory64.EnclaveConfigurationPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.VolatileMetadataPointer),indent} = {data.LoadConfigDirectory64.VolatileMetadataPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardEHContinuationTable),indent} = {data.LoadConfigDirectory64.GuardEHContinuationTable}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardEHContinuationCount),indent} = 0x{data.LoadConfigDirectory64.GuardEHContinuationCount}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardXFGCheckFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardXFGCheckFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardXFGDispatchFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardXFGDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardXFGTableDispatchFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardXFGTableDispatchFunctionPointer}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.CastGuardOsDeterminedFailureMode),indent} = {data.LoadConfigDirectory64.CastGuardOsDeterminedFailureMode}");
            writer.WriteLine($"            {nameof(PELoadConfigDirectory64.GuardMemcpyFunctionPointer),indent} = {data.LoadConfigDirectory64.GuardMemcpyFunctionPointer}");
        }
    }

    private static void Print(PEFile file, PEResourceDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PETlsDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEDataDirectory data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEBoundImportAddressTable32 data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEBoundImportAddressTable64 data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEDelayImportAddressTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEExportAddressTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEExportNameTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEExportOrdinalTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEImportAddressTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEImportLookupTable data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

    }

    private static void Print(PEFile file, PEStreamSectionData data, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

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