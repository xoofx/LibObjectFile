# Changelog

## 0.3.2 (22 Dec 2019)
- Fix a bug when reading ElfObjectFile from an existing ELF where ObjectFile.FileClass/Encoding/Version/OSABI/AbiVersion was not actually deserialized.

## 0.3.1 (18 Dec 2019)
- Fix creation of DWARF sections from scratch

## 0.3.0 (18 Dec 2019)
- Add support for DWARF Version 4 (missing only .debug_frame)
- Add support for layouting sections independently of the order defined sections header

## 0.2.1 (17 Nov 2019)
- Add verify for PT_LOAD segment align requirements

## 0.2.0 (17 Nov 2019)
- Add support for ElfNoteTable
- Add XML documentation and user manual
- Removed some accessors that should not have been public

## 0.1.0 (16 Nov 2019)
- Initial version with support for ELF file format