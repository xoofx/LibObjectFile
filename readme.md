# LibObjectFile [![ci](https://github.com/xoofx/LibObjectFile/actions/workflows/CI.yml/badge.svg)](https://github.com/xoofx/LibObjectFile/actions/workflows/CI.yml) [![NuGet](https://img.shields.io/nuget/v/LibObjectFile.svg)](https://www.nuget.org/packages/LibObjectFile/)

<img align="right" width="200px" height="200px" src="https://raw.githubusercontent.com/xoofx/LibObjectFile/master/img/libobjectfile.png">

LibObjectFile is a .NET library to read, manipulate and write linker and executable object files (e.g ELF, ar, DWARF, PE...)

> NOTE: Currently LibObjectFile supports the following file format:
>
> - **PE** image file format (Portable Executable / DLL)
> - **ELF** object-file format
> - **Mach-O** object-file format, including universal binaries
> - **DWARF** debugging format (version 4)
> - **Archive `ar`** file format (Common, GNU and BSD variants)
>
> There is a longer term plan to support other file formats (e.g COFF, .lib) but as I don't 
> have a need for them right now, it is left as an exercise for PR contributors! ;)

## Usage

```C#
// Reads an ELF file
using var inStream = File.OpenRead("helloworld");
var elf = ElfFile.Read(inStream);
foreach(var section in elf.Sections)
{
    Console.WriteLine(section.Name);
}
// Print the content of the ELF as readelf output
elf.Print(Console.Out);
// Write the ElfFile to another file on the disk
using var outStream = File.OpenWrite("helloworld2");
elf.Write(outStream);
```

## Features
- Full support of **Archive `ar` file format** including Common, GNU and BSD variants.
- Full support for the **PE file format**
  - Support byte-to-byte roundtrip
  - Read and write from/to a `System.IO.Stream`
  - All PE Directories are supported
  - `PEFile.Relocate` to relocate the image base of a PE file
  - `PEFile.Print` to print the content of a PE file to a textual representation
  - Support for calculating the checksum of a PE file
- Good support for the **ELF file format**:
  - Support byte-to-byte roundtrip
  - Read and write from/to a `System.IO.Stream`
  - Handling of LSB/MSB
  - Support the following sections: 
    - String Table
    - Symbol Table
    - Relocation Table: supported I386, X86_64, ARM and AARCH64 relocations (others can be exposed by adding some mappings)
    - Note Table
    - Dynamic Linking Table (`SHT_DYNAMIC`)
    - Other sections fallback to `ElfCustomSection`
  - Program headers with or without sections
  - `ElfFile.AddNeededLibrary` injects a `DT_NEEDED` dependency into an existing image without moving any section (address-preserving, `patchelf`-style)
  - Print with `readelf` similar output
- Good support for the **Mach-O file format**:
  - Support byte-to-byte roundtrip
  - Read and write from/to a `System.IO.Stream`
  - 32 and 64 bit, `i386`, `x86_64` and `arm64`
  - Universal (fat) binaries via `MachOFatFile`
  - All load commands are decoded, and commands this library does not model round-trip verbatim
  - Symbol table, indirect symbol table and relocations
  - `MachOFile.AddLoadDylib`, `AddRPath` and `ChangeDylibName` edit an existing image without moving any section (address-preserving, `install_name_tool`-style)
  - `MachOFile.AdHocSign` writes an ad-hoc code signature, which Apple Silicon requires in order to execute an image
  - `MachOFile.Print` to print the content of a Mach-O file with `otool` similar output
- Support for **DWARF debugging format**:
  - Partial support of Version 4 (currently still the default for GCC)
  - Support for the sections: `.debug_info`, `.debug_line`, `.debug_aranges`, `.debug_abbrev` and `.debug_str` 
  - Support for Dwarf expressions
  - High level interface, automatic layout/offsets between sections.
  - Integration with ELF to support easy reading/writing back
  - Support for relocatable sections
- Use of a Diagnostics API to validate file format (on read/before write)
- Library requiring `net8.0`
    - If you are looking for `netstandard2.1` support you will need to use `0.4.0` version

## Documentation

The [doc/readme.md](doc/readme.md) explains how the library is designed and can be used.

## Download

LibObjectFile is available as a NuGet package: [![NuGet](https://img.shields.io/nuget/v/LibObjectFile.svg)](https://www.nuget.org/packages/LibObjectFile/)

## Build

In order to build LibObjectFile, you need to have installed the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download).

Running the tests require Ubuntu 22.04. `dotnet test` will work on Windows (via WSL) and on that version of Ubuntu.
If you're using macOS or another Linux, there's a Dockerfile and a helper script under `src` to run tests in the right OS version.

## License

This software is released under the [BSD-Clause 2 license](https://github.com/xoofx/LibObjectFile/blob/master/license.txt).

## Author

Alexandre MUTEL aka [xoofx](https://xoofx.github.io)

## Supporters

Supports this project with a monthly donation and help me continue improving it. \[[Become a supporter](https://github.com/sponsors/xoofx)\]

[<img src="https://github.com/bruno-garcia.png?size=200" width="64px;" style="border-radius: 50%" alt="bruno-garcia"/>](https://github.com/bruno-garcia)
