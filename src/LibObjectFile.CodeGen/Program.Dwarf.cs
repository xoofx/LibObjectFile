// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using CppAst.CodeGen.CSharp;

namespace LibObjectFile.CodeGen
{
    partial class Program
    {
        private static void GenerateDwarf()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "DwarfNative",
                DefaultNamespace = "LibObjectFile.Dwarf",
                DefaultOutputFilePath = "/LibObjectFile.Dwarf.generated.cs",
                DefaultDllImportNameAndArguments = "NotUsed",
                MappingRules =
                {
                    map => map.MapMacroToConst("^DW_TAG_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_FORM_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_AT_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_LN[ES]_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_IDX_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_LANG_.*", "unsigned short"),
                    map => map.MapMacroToConst("^DW_ID_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_CC_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_ISA_.*", "unsigned char"),
                    map => map.MapMacroToConst("^DW_CHILDREN_.*", "unsigned char"),
                }
            };

            cppOptions.GenerateEnumItemAsFields = false;
            cppOptions.IncludeFolders.Add(Environment.CurrentDirectory);

            var csCompilation = CSharpConverter.Convert(@"#include ""dwarf.h""", cppOptions);

            AssertCompilation(csCompilation);

            // Add pragma
            var csFile = csCompilation.Members.OfType<CSharpGeneratedFile>().First();
            var ns = csFile.Members.OfType<CSharpNamespace>().First();
            csFile.Members.Insert(csFile.Members.IndexOf(ns), new CSharpLineElement("#pragma warning disable 1591") );

            ProcessElfEnum(cppOptions, csCompilation, "DW_AT_", "DwarfAttributeName");
            ProcessElfEnum(cppOptions, csCompilation, "DW_FORM_", "DwarfAttributeForm");
            ProcessElfEnum(cppOptions, csCompilation, "DW_TAG_", "DwarfTag");

            csCompilation.DumpTo(GetCodeWriter(Path.Combine("LibObjectFile", "generated")));
        }
    }
}