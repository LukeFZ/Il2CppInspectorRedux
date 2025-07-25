// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// Copyright (c) 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// Copyright (c) 2023 LukeFZ https://github.com/LukeFZ
// All rights reserved

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Properties;

namespace Il2CppInspector.Outputs
{
    public partial class CppScaffolding(AppModel model, bool useBetterArraySize = false)
    {
        private readonly AppModel _model = model;

        /*
         * 2017.2.1 changed the type of il2cpp_array_size_t to uintptr_t from int32_t. The code, however, uses static_cast<int32_t>(maxLength) to access this value,
         * which makes decompilation a bit unpleasant due to it only ever checking the lower 32 bits.
         * The better array size type is a union of the actual size (int32_t) and the actual value (uintptr_t) which should hopefully improve decompilation.
         */
        private readonly bool _useBetterArraySize = 
            model.UnityVersion.CompareTo("2017.2.1") >= 0 
            && model.Package.BinaryImage.Bits == 64 
            && useBetterArraySize;

        private StreamWriter _writer;

        // Write the type header
        // This can be used by other output modules
        public void WriteTypes(string typeHeaderFile) {
            using var fs = new FileStream(typeHeaderFile, FileMode.Create);
            _writer = new StreamWriter(fs, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();

                // Write primitive type definitions for when we're not including other headers
                writeCode($"""
                       #if defined(_IDACLANG_) || defined(_BINARYNINJA_)
                       #define IS_LIBCLANG_DECOMPILER
                       #endif
                       
                       #if defined(_GHIDRA_) || defined(_IDA_) || defined(IS_LIBCLANG_DECOMPILER)
                       #define IS_DECOMPILER
                       #endif

                       #if defined(_GHIDRA_) || defined(_IDA_)
                       typedef unsigned __int8 uint8_t;
                       typedef unsigned __int16 uint16_t;
                       typedef unsigned __int32 uint32_t;
                       typedef unsigned __int64 uint64_t;
                       typedef __int8 int8_t;
                       typedef __int16 int16_t;
                       typedef __int32 int32_t;
                       typedef __int64 int64_t;
                       #endif
                       
                       #if defined(IS_LIBCLANG_DECOMPILER)
                       typedef unsigned char uint8_t;
                       typedef unsigned short uint16_t;
                       typedef unsigned int uint32_t;
                       typedef unsigned long uint64_t;
                       typedef char int8_t;
                       typedef short int16_t;
                       typedef int int32_t;
                       typedef long int64_t;
                       
                       #ifdef linux
                       #undef linux
                       #endif
                       
                       #endif
                       
                       #if defined(_GHIDRA_) || defined(IS_LIBCLANG_DECOMPILER)
                       typedef int{_model.Package.BinaryImage.Bits}_t intptr_t;
                       typedef uint{_model.Package.BinaryImage.Bits}_t uintptr_t;
                       typedef uint{_model.Package.BinaryImage.Bits}_t size_t;
                       #endif

                       #ifndef IS_DECOMPILER
                       #define _CPLUSPLUS_
                       #endif
                       """);

                if (_useBetterArraySize)
                    writeCode("#define il2cpp_array_size_t actual_il2cpp_array_size_t");

                writeSectionHeader("IL2CPP internal types");
                writeCode(_model.UnityHeaders.GetTypeHeaderText(_model.WordSizeBits));

                if (_useBetterArraySize)
                    writeCode("""
                          #undef il2cpp_array_size_t
                          
                          typedef union better_il2cpp_array_size_t
                          {
                               int32_t size;
                               actual_il2cpp_array_size_t value;
                          } il2cpp_array_size_t;
                          """);

                if (_model.TargetCompiler == CppCompilerType.MSVC)
                {
                    // Stop MSVC complaining about out-of-bounds enum values
                    writeCode("#pragma warning(disable : 4369)");

                    // Stop MSVC complaining about constant truncation of enum values
                    writeCode("#pragma warning(disable : 4309)");

                    // MSVC will (rightly) throw a compiler warning when compiling for 32-bit architectures
                    // if the specified alignment of a type is smaller than the size of its largest element.
                    // We keep the alignments in to make them match Il2CppObject wherever possible, but it is
                    // safe to ignore them if they are too small, so we just disable the warning
                    writeCode("#pragma warning(disable : 4359)");
                }

                // C does not support namespaces
                writeCode("#ifndef IS_DECOMPILER");
                writeCode("namespace app {");
                writeCode("#endif");
                writeLine("");

                writeForwardDefinitions();

                writeTypesForGroup("Required forward definitions", "required_forward_definitions");
                writeTypesForGroup("Application types from method calls", "types_from_methods");
                writeTypesForGroup("Application types from generic methods", "types_from_generic_methods");
                writeTypesForGroup("Application types from usages", "types_from_usages");
                writeTypesForGroup("Application unused value types", "unused_concrete_types");

                writeCode("#ifndef IS_DECOMPILER");
                writeCode("}");
                writeCode("#endif");
            }
        }

        public void Write(string projectPath) {
            // Ensure output directory exists and is not a file
            // A System.IOException will be thrown if it's a file'
            var srcUserPath = Path.Combine(projectPath, "user");
            var srcFxPath = Path.Combine(projectPath, "framework");
            var srcDataPath = Path.Combine(projectPath, "appdata");

            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(srcUserPath);
            Directory.CreateDirectory(srcFxPath);
            Directory.CreateDirectory(srcDataPath);

            // Write type definitions to il2cpp-types.h
            WriteTypes(Path.Combine(srcDataPath, "il2cpp-types.h"));

            // Write selected Unity API function file to il2cpp-api-functions.h
            // (this is a copy of the header file from an actual Unity install)
            var il2cppApiFile = Path.Combine(srcDataPath, "il2cpp-api-functions.h");
            var apiHeaderText = _model.UnityHeaders.GetAPIHeaderText();

            using var fsApi = new FileStream(il2cppApiFile, FileMode.Create);
            _writer = new StreamWriter(fsApi, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();

                // Elide APIs that aren't in the binary to avoid compile errors
                foreach (var line in apiHeaderText.Split('\n'))
                {
                    var fnName = UnityHeaders.GetFunctionNameFromAPILine(line);

                    if (string.IsNullOrEmpty(fnName))
                        _writer.WriteLine(line);
                    else if (_model.AvailableAPIs.ContainsKey(fnName))
                        _writer.WriteLine(line);
                }
            }

            // Write API function pointers to il2cpp-api-functions-ptr.h
            var il2cppFnPtrFile = Path.Combine(srcDataPath, "il2cpp-api-functions-ptr.h");

            using var fs2 = new FileStream(il2cppFnPtrFile, FileMode.Create);
            _writer = new StreamWriter(fs2, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();
                writeSectionHeader("IL2CPP API function pointers");

                // We could use _model.AvailableAPIs here but that would exclude outputting the address
                // of API exports which for some reason aren't defined in our selected API header,
                // so although it doesn't affect the C++ compilation, we use GetAPIExports() instead for completeness
                var exports = _model.Package.Binary.APIExports;

                foreach (var export in exports)
                {
                    writeCode($"#define {export.Key}_ptr 0x{_model.Package.BinaryImage.MapVATR(export.Value):X8}");
                }
            }

            // Write application type definition addresses to il2cpp-types-ptr.h
            var il2cppTypeInfoFile = Path.Combine(srcDataPath, "il2cpp-types-ptr.h");

            using var fs3 = new FileStream(il2cppTypeInfoFile, FileMode.Create);
            _writer = new StreamWriter(fs3, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();
                writeSectionHeader("IL2CPP application-specific type definition addresses");

                foreach (var type in _model.Types.Values.Where(t => t.TypeClassAddress != 0xffffffff_ffffffff))
                {
                    writeCode($"DO_TYPEDEF(0x{type.TypeClassAddress - _model.Package.BinaryImage.ImageBase:X8}, {type.Name});");
                }
            }

            // Write method pointers and signatures to il2cpp-functions.h
            var methodFile = Path.Combine(srcDataPath, "il2cpp-functions.h");

            using var fs4 = new FileStream(methodFile, FileMode.Create);
            _writer = new StreamWriter(fs4, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();
                writeSectionHeader("IL2CPP application-specific method definition addresses and signatures");

                writeCode("using namespace app;");
                writeLine("");

                foreach (var method in _model.Methods.Values)
                {
                    if (method.HasCompiledCode)
                    {
                        var arguments = string.Join(", ", method.CppFnPtrType.Arguments.Select(a => a.Type.Name + " " + (a.Name == "this" ? "__this" : a.Name)));

                        writeCode($"DO_APP_FUNC(0x{method.MethodCodeAddress - _model.Package.BinaryImage.ImageBase:X8}, {method.CppFnPtrType.ReturnType.Name}, "
                                  + $"{method.CppFnPtrType.Name}, ({arguments}));");
                    }

                    if (method.HasMethodInfo)
                    {
                        writeCode($"DO_APP_FUNC_METHODINFO(0x{method.MethodInfoPtrAddress - _model.Package.BinaryImage.ImageBase:X8}, {method.CppFnPtrType.Name}__MethodInfo);");
                    }
                }
            }

            // Write metadata version
            var versionFile = Path.Combine(srcDataPath, "il2cpp-metadata-version.h");

            using var fs5 = new FileStream(versionFile, FileMode.Create);
            _writer = new StreamWriter(fs5, Encoding.ASCII);

            using (_writer)
            {
                writeHeader();
                writeCode($"#define __IL2CPP_METADATA_VERSION {_model.Package.Version.Major * 10 + _model.Package.Version.Minor * 10:F0}");
            }

            // Write boilerplate code
            File.WriteAllText(Path.Combine(srcFxPath, "dllmain.cpp"), Resources.Cpp_DLLMainCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "helpers.cpp"), Resources.Cpp_HelpersCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "helpers.h"), Resources.Cpp_HelpersH);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-appdata.h"), Resources.Cpp_Il2CppAppDataH);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-init.cpp"), Resources.Cpp_Il2CppInitCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-init.h"), Resources.Cpp_Il2CppInitH);
            File.WriteAllText(Path.Combine(srcFxPath, "pch-il2cpp.cpp"), Resources.Cpp_PCHIl2Cpp);
            File.WriteAllText(Path.Combine(srcFxPath, "pch-il2cpp.h"), Resources.Cpp_PCHIl2CppH);

            // Write user code without overwriting existing code
            void WriteIfNotExists(string path, string contents) { if (!File.Exists(path)) File.WriteAllText(path, contents); }

            WriteIfNotExists(Path.Combine(srcUserPath, "main.cpp"), Resources.Cpp_MainCpp);
            WriteIfNotExists(Path.Combine(srcUserPath, "main.h"), Resources.Cpp_MainH);

            // Write Visual Studio project and solution files
            var projectGuid = Guid.NewGuid();
            var projectName = "IL2CppDLL";
            var projectFile = projectName + ".vcxproj";

            WriteIfNotExists(Path.Combine(projectPath, projectFile),
                Resources.CppProjTemplate.Replace("%PROJECTGUID%", projectGuid.ToString()));

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();
            var filtersFile = projectFile + ".filters";

            var filters = Resources.CppProjFilters
                .Replace("%GUID1%", guid1.ToString())
                .Replace("%GUID2%", guid2.ToString())
                .Replace("%GUID3%", guid3.ToString());

            WriteIfNotExists(Path.Combine(projectPath, filtersFile), filters);

            var solutionGuid = Guid.NewGuid();
            var solutionFile = projectName + ".sln";

            var sln = Resources.CppSlnTemplate
                .Replace("%PROJECTGUID%", projectGuid.ToString())
                .Replace("%PROJECTNAME%", projectName)
                .Replace("%PROJECTFILE%", projectFile)
                .Replace("%SOLUTIONGUID%", solutionGuid.ToString());

            WriteIfNotExists(Path.Combine(projectPath, solutionFile), sln);
        }

        private void writeHeader() {
            writeLine("// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("// Target Unity version: " + _model.UnityHeaders);
            writeLine("");
        }

        private void writeForwardDefinitions()
        {
            writeSectionHeader("Required forward definitions");
            foreach (var cppType in _model.RequiredForwardDefinitions)
                writeCode(cppType.ToString());
        }

        private void writeTypesForGroup(string header, string group) 
        {
            writeSectionHeader(header);
            foreach (var cppType in _model.GetDependencyOrderedCppTypeGroup(group))
            {
                if (cppType is CppEnumType)
                {
                    // Ghidra can't process C++ enum base types
                    writeCode("#if defined(_CPLUSPLUS_)");
                    writeCode(cppType.ToString());
                    writeCode("#else");
                    writeCode(cppType.ToString("c"));
                    writeCode("#endif");
                }
                else
                {
                    writeCode(cppType.ToString());
                }
            }
        }
        
        private void writeCode(string text) {
            if (_model.TargetCompiler == CppCompilerType.MSVC)
                text = GccAlignRegex().Replace(text, @"__declspec(align($1))");
            else if (_model.TargetCompiler == CppCompilerType.GCC)
                text = MsvcAlignRegex().Replace(text, @"__attribute__((aligned($1)))");

            var lines = text.Replace("\r", "").Split('\n');
            //var cleanLines = lines.Select(s => s.ToEscapedString()); Not sure if this is necessary? maybe for some obfuscated assemblies, but those would just fail on other steps

            foreach (var line in lines)
                writeLine(line);
        }

        private void writeSectionHeader(string name) {
            writeLine("// ******************************************************************************");
            writeLine("// * " + name);
            writeLine("// ******************************************************************************");
            writeLine("");
        }

        private void writeLine(string line) => _writer.WriteLine(line);

        [GeneratedRegex(@"__attribute__\s*?\(\s*?\(\s*?aligned\s*?\(\s*?([0-9]+)\s*?\)\s*?\)\s*?\)")]
        private static partial Regex GccAlignRegex();

        [GeneratedRegex(@"__declspec\s*?\(\s*?align\s*?\(\s*?([0-9]+)\s*?\)\s*?\)")]
        private static partial Regex MsvcAlignRegex();
    }
}
