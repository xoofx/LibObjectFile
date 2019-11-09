using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CppAst;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using StarkPlatform.Reflection.Metadata;
using Zio.FileSystems;

namespace StarkPlatform.NativeCompiler.CodeGen
{
    class Program
    {
        private const string SrcFolderRelative = @"..\..\..\..";

        static void Main(string[] args)
        {
            GenerateElf();
            GenerateClrJit();
            GenerateCore();
            GenerateOpCodeToIROpCode();
        }

        private static CodeWriter GetCodeWriterForNcl()
        {
            return GetCodeWriter(Path.Combine("StarkPlatform.NativeCompiler", "generated"));
        }
        
        private static CodeWriter GetCodeWriter(string subPath)
        {
            var fs = new PhysicalFileSystem();
            var destFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, SrcFolderRelative, subPath));
            var subfs = new SubFileSystem(fs, fs.ConvertPathFromInternal(destFolder));
            var codeWriter = new CodeWriter(new CodeWriterOptions(subfs));
            return codeWriter;
        }

        private static void AssertCompilation(CSharpCompilation csCompilation)
        {
            if (csCompilation.HasErrors)
            {
                foreach (var message in csCompilation.Diagnostics.Messages)
                {
                    Console.Error.WriteLine(message);
                }
                Console.Error.WriteLine("Unexpected parsing errors");
                Environment.Exit(1);
            }
        }

        private static void GenerateClrJit()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "LibStarkNcl",
                DefaultNamespace = "StarkPlatform.NativeCompiler",
                DefaultOutputFilePath = "/LibStarkNcl.generated.cs",
                DefaultDllImportNameAndArguments = "StarkNclName",
                MappingRules =
                {
                }
            };

            cppOptions.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);
            cppOptions.Defines.Add("_AMD64_");
            cppOptions.Defines.Add("_TARGET_AMD64_");
            cppOptions.Defines.Add("STARK_NO_ENUM_FLAG");
            cppOptions.GenerateEnumItemAsFields = false;

            var folder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $@"{SrcFolderRelative}\stark-ncl"));
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"The stark-ncl directory `{folder}` doesn't exist'");
            }

            cppOptions.IncludeFolders.Add(folder);

            var csCompilation = CSharpConverter.Convert(@"#include ""stark-ncl.h""", cppOptions);

            AssertCompilation(csCompilation);

            csCompilation.DumpTo(GetCodeWriterForNcl());
        }

        private static void GenerateElf()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "RawElf",
                DefaultNamespace = "LibObjectFile.Elf",
                DefaultOutputFilePath = "/LibObjectFile.Elf.generated.cs",
                DefaultDllImportNameAndArguments = "NotUsed",
                GenerateAsInternal = true,
                MappingRules =
                {
                    map => map.MapMacroToConst("^EI.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFMAG\\d", "uint8_t"),
                    map => map.MapMacroToConst("^ELFCLASS.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFDATA.*", "uint8_t"),
                    map => map.MapMacroToConst("^ELFOSABI.*", "uint8_t"),
                    map => map.MapMacroToConst("^ET_.*", "uint16_t"),
                    map => map.MapMacroToConst("^EM_.*", "uint16_t"),
                    map => map.MapMacroToConst("^EV_.*", "uint8_t"),
                    map => map.MapMacroToConst("^SHN_.*", "uint32_t"),
                    map => map.MapMacroToConst("^SHT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^SHF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^EF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^PT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^PF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^NT_.*", "uint32_t"),
                    map => map.MapMacroToConst("^DT_.*", "int32_t"),
                    map => map.MapMacroToConst("^DF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^DTF_.*", "uint32_t"),
                    map => map.MapMacroToConst("^VER_DEF_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_FLG_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_NDX_.*", "uint16_t"),
                    map => map.MapMacroToConst("^VER_NEED_.*", "uint16_t"),
                    map => map.MapMacroToConst("^ELFCOMPRESS_.*", "int32_t"),
                    map => map.MapMacroToConst("^SYMINFO_.*", "uint16_t"),
                    map => map.MapMacroToConst("^STB_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STT_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STN_.*", "uint8_t"),
                    map => map.MapMacroToConst("^STV_.*", "uint8_t"),
                    map => map.MapMacroToConst("^R_386_.*", "uint32_t"),
                    map => map.MapMacroToConst("^R_ARM_.*", "uint32_t"),
                    map => map.MapMacroToConst("^R_AARCH64_.*", "uint32_t"),
                    
                }
            };

            cppOptions.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);
            cppOptions.Defines.Add("_AMD64_");
            cppOptions.Defines.Add("_TARGET_AMD64_");
            cppOptions.Defines.Add("STARK_NO_ENUM_FLAG");
            cppOptions.GenerateEnumItemAsFields = false;
            cppOptions.IncludeFolders.Add(Environment.CurrentDirectory);

            var csCompilation = CSharpConverter.Convert(@"#include ""elf.h""", cppOptions);

            AssertCompilation(csCompilation);


            var ns = csCompilation.Members.OfType<CSharpGeneratedFile>().First().Members.OfType<CSharpNamespace>().First();

            var rawElfClass = ns.Members.OfType<CSharpClass>().First();

            var machines = rawElfClass.Members.OfType<CSharpField>().Where(x => (x.Modifiers & CSharpModifiers.Const) != 0 && x.Name.StartsWith("EM_")).ToList();

            var elfArchClass = new CSharpStruct("ElfArch")
            {
                Modifiers = CSharpModifiers.Partial | CSharpModifiers.ReadOnly
            };
            ns.Members.Add(elfArchClass);

            var machineToArchFields = new List<(CSharpField, CSharpField)>();

            foreach (var machine in machines)
            {
                var machineName = machine.Name;

                // Don't add EM_NUM
                if (machineName == "EM_NUM") continue;

                var csArchName = machineName.Substring(3); // discard EM_
                switch (csArchName)
                {
                    case "386": csArchName = "I386"; break;
                    case "88K": csArchName = "M88K"; break;
                    case "860": csArchName = "I860"; break;
                    case "960": csArchName = "I960"; break;
                    default:
                        // assume Motorola
                        if (csArchName.StartsWith("68"))
                        {
                            csArchName = $"M{csArchName}";
                        }
                        break;
                }

                if (char.IsDigit(csArchName[0]))
                {
                    throw new InvalidOperationException($"The machine name `{machineName}` starts with a number and needs to be modified");
                }

                var archField = new CSharpField(csArchName)
                {
                    Modifiers = CSharpModifiers.Static | CSharpModifiers.ReadOnly, 
                    FieldType = elfArchClass, 
                    Visibility = CSharpVisibility.Public,
                    Comment = machine.Comment,
                    InitValue = $"new {elfArchClass.Name}({cppOptions.DefaultClassLib}.{machineName})"
                };

                machineToArchFields.Add((machine, archField));

                elfArchClass.Members.Add(archField);
            }

            var toStringInternal = new CSharpMethod()
            {
                Name = "ToStringInternal",
                Visibility = CSharpVisibility.Private,
                ReturnType = CSharpPrimitiveType.String
            };
            elfArchClass.Members.Add(toStringInternal);

            toStringInternal.Body = (writer, element) =>
            {
                writer.WriteLine("switch (Value)");
                writer.OpenBraceBlock();
                foreach (var machineAndArch in machineToArchFields)
                {
                    var machineField = machineAndArch.Item1;
                    var archField = machineAndArch.Item2;
                    var cppField = ((CppField) machineField.CppElement);
                    var descriptionText = cppField.Comment?.ToString().Replace("\"", "\\\"") ?? machineField.Name;
                    descriptionText = descriptionText.Replace("\r\n", "").Replace("\n", "");
                    writer.WriteLine($"case {cppOptions.DefaultClassLib}.{machineField.Name}: return \"{descriptionText}\";");
                }

                writer.WriteLine($"default: return \"Unknown Arch\";");
                writer.CloseBraceBlock();
            };

            csCompilation.DumpTo(GetCodeWriter(Path.Combine("LibObjectFile", "generated")));
        }

        private static void GenerateCore()
        {
            var cppOptions = new CSharpConverterOptions()
            {
                DefaultClassLib = "lib_stark_ncl_core",
                DefaultNamespace = "StarkPlatform.NativeCompiler",
                DefaultOutputFilePath = "/LibStarkNclCore.generated.cs",
                DefaultDllImportNameAndArguments = "StarkNclCoreName",
                MappingRules =
                {
                }
            };

            cppOptions.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);

            var folder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $@"{SrcFolderRelative}\stark-ncl-clrjit\stark-ncl-core"));
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"The stark-ncl directory `{folder}` doesn't exist'");
            }

            cppOptions.IncludeFolders.Add(folder);

            var csCompilation = CSharpConverter.Convert(@"#include ""stark-ncl-core.h""", cppOptions);

            AssertCompilation(csCompilation);

            csCompilation.DumpTo(GetCodeWriterForNcl());
        }


        private static void GenerateOpCodeToIROpCode()
        {
            var names = Enum.GetNames(typeof(ILOpCode));
            var values = Enum.GetValues(typeof(ILOpCode));
            var destFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $@"{SrcFolderRelative}\StarkPlatform.NativeCompiler\generated"));
            var destFile = Path.Combine(destFolder, "IROpCode.generated.cs");
            Directory.CreateDirectory(destFolder);

            int count = (int)unchecked((byte)ILOpCode.Readonly) + 0xE2 + 1;
            var mapping = new byte[count];
            var mappingToILOpCode = new ushort[count];
            var isMapped = new bool[count];
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = (int)(ushort)values.GetValue(i);
                if (value >= 0xFE00)
                {
                    value = value - 0xfe00 + 1 + (int)ILOpCode.Ldtarg;
                }
                mapping[value] = (byte)i;
                isMapped[value] = true;
                mappingToILOpCode[value] = (ushort)values.GetValue(i);
            }

            var irnames = Enum.GetNames(typeof(IROpCode));
            var irvalues = Enum.GetValues(typeof(IROpCode));
            var mapIRToIL = new ILOpCode[irnames.Length - 2];
            for (var i = 0; i < irnames.Length; i++)
            {
                Debug.Assert((int)irvalues.GetValue(i) == i);
                if (i >= (int)IROpCode.Count) break;
                mapIRToIL[i] = (ILOpCode)Enum.Parse(typeof(ILOpCode), irnames[i]);
            }

            using (var writer = new StreamWriter(destFile))
            {
                writer.Write($@"using System;
using System.Runtime.CompilerServices;
using StarkPlatform.Reflection.Metadata;

namespace StarkPlatform.NativeCompiler
{{
    public static partial class IROpCodeExtensions
    {{
        private static ReadOnlySpan<byte> MapOpCodeToIROpCode => new ReadOnlySpan<byte>(new byte[{count}]
        {{
");
                for (var i = 0; i < mapping.Length; i++)
                {
                    var mapIndex = mapping[i];
                    var comma = i + 1 == mapping.Length ? string.Empty : ",";
                    var padding = 15 - names[mapIndex].Length + (comma == string.Empty ? 1 : 0);
                    if (isMapped[i])
                    {
                        writer.WriteLine($"            (byte)IROpCode.{names[mapIndex]}{comma}{new string(' ', padding)}// {mapIndex} => 0x{mappingToILOpCode[i]:X4}");
                    }
                    else
                    {
                        writer.WriteLine($"            0{comma}");
                    }
                }

                writer.WriteLine(@"        });");

                writer.Write($@"        private static ReadOnlySpan<byte> MapIROpCodeToILOpCode => new ReadOnlySpan<byte>(new byte[{mapIRToIL.Length}]
        {{
");
                for (var i = 0; i < mapIRToIL.Length; i++)
                {
                    var mapIndex = mapIRToIL[i];
                    var comma = i + 1 == mapping.Length ? string.Empty : ",";
                    var name = mapIndex.ToString();
                    var padding = 15 - name.Length + (comma == string.Empty ? 1 : 0);
                    writer.WriteLine($"            unchecked((byte)ILOpCode.{name}){comma}{new string(' ', padding)}// IROpCode.{name} = {i}");
                }

                writer.WriteLine(@"        });");

                writer.WriteLine();

                writer.Write($@"        private static readonly string[] MapIROpCodeToText = new string[{mapIRToIL.Length}]
        {{
");
                for (var i = 0; i < mapIRToIL.Length; i++)
                {
                    var mapIndex = mapIRToIL[i];
                    var comma = i + 1 == mapping.Length ? string.Empty : ",";
                    var name = mapIndex.ToString();
                    var padding = 15 - name.Length + (comma == string.Empty ? 1 : 0);
                    writer.WriteLine($"            \"{name.ToLowerInvariant().Replace('_', '.')}\"{comma}{new string(' ', padding)}// IROpCode.{name}");
                }

                writer.WriteLine(@"        };");


                writer.WriteLine(@"    }
}");



            }
        }
    }
}