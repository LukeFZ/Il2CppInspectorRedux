/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.Next;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppInspector.Reflection
{
    // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.customattributedata?view=netframework-4.8
    public class CustomAttributeData
    {
        // IL2CPP-specific data
        public TypeModel Model => AttributeType.Assembly.Model;
        public int Index { get; set; }

        // The type of the attribute
        public TypeInfo AttributeType { get; set; }

        // v29 custom attribute info
        public CustomAttributeCtor CtorInfo { get; set; }

        // Pre-v29 Properties used for stub Attributes

        public (ulong Start, ulong End) VirtualAddress => CtorInfo != null ? (0, 0) :
            // The last one will be wrong but there is no way to calculate it
            (Model.Package.CustomAttributeGenerators[Index], Model.Package.FunctionAddresses[Model.Package.CustomAttributeGenerators[Index]]);

        // C++ method names
        // TODO: Known issue here where we should be using CppDeclarationGenerator.TypeNamer to ensure uniqueness
        public string Name => $"{AttributeType.Name.ToCIdentifier()}_CustomAttributesCacheGenerator";

        // C++ method signature
        public string Signature => $"void {Name}(CustomAttributesCache *)";

        public override string ToString() => "[" + AttributeType.FullName + "]";

        // Get the machine code of the C++ function
        public byte[] GetMethodBody() => Model.Package.BinaryImage.ReadMappedBytes(VirtualAddress.Start, (int) (VirtualAddress.End - VirtualAddress.Start));

        public IEnumerable<TypeInfo> GetAllTypeReferences()
        {
            yield return AttributeType;

            if (CtorInfo != null)
            {
                foreach (var typeRef in GetTypeReferences(CtorInfo.Arguments))
                    yield return typeRef;

                foreach (var typeRef in GetTypeReferences(CtorInfo.Fields))
                    yield return typeRef;

                foreach (var typeRef in GetTypeReferences(CtorInfo.Properties))
                    yield return typeRef;
            }

            yield break;

            static IEnumerable<TypeInfo> GetTypeReferences(IEnumerable<CustomAttributeArgument> arguments)
            {
                foreach (var arg in arguments)
                {
                    yield return arg.Type;

                    switch (arg.Value)
                    {
                        case TypeInfo info:
                            yield return info;
                            break;
                        case CustomAttributeArgument[] array:
                            foreach (var info in GetTypeReferences(array))
                                yield return info;
                            break;
                    }
                }
            }
        }

        // Get all the custom attributes for a given assembly, type, member or parameter
        private static IEnumerable<CustomAttributeData> getCustomAttributes(Assembly asm, int customAttributeIndex) {
            if (customAttributeIndex < 0)
                yield break;

            var pkg = asm.Model.Package;

            // Attribute type ranges weren't included before v21 (customASttributeGenerators was though)
            if (pkg.Version < MetadataVersions.V210)
                yield break;

            if (pkg.Version < MetadataVersions.V290)
            {
                var range = pkg.AttributeTypeRanges[customAttributeIndex];
                for (var i = range.Start; i < range.Start + range.Count; i++)
                {
                    var typeIndex = pkg.AttributeTypeIndices[i];

                    if (asm.Model.AttributesByIndices.TryGetValue(i, out var attribute))
                    {
                        yield return attribute;
                        continue;
                    }

                    attribute = new CustomAttributeData { Index = customAttributeIndex, AttributeType = asm.Model.TypesByReferenceIndex[typeIndex] };

                    asm.Model.AttributesByIndices.TryAdd(i, attribute);
                    yield return attribute;
                }
            }
            else
            {
                if (!asm.Model.AttributesByDataIndices.TryGetValue(customAttributeIndex, out var attributes))
                {
                    try
                    {
                        var range = pkg.Metadata.AttributeDataRanges[customAttributeIndex];
                        var next = pkg.Metadata.AttributeDataRanges[customAttributeIndex + 1];

                        // The sentinel entry after the last real range should hold the blob's total
                        // size; fall back to that if it's stale instead of underflowing the length.
                        var nextStartOffset = next.StartOffset > range.StartOffset
                            ? next.StartOffset
                            : (uint)pkg.Metadata.AttributeDataSize;

                        var startOffset = (uint)pkg.Metadata.AttributeDataOffset + range.StartOffset;
                        var endOffset = (uint)pkg.Metadata.AttributeDataOffset + nextStartOffset;

                        var reader = new CustomAttributeDataReader(pkg, asm, pkg.Metadata, startOffset, endOffset);
                        if (reader.Count == 0)
                        {
                            attributes = new List<CustomAttributeData>();
                        }
                        else
                        {
                            attributes = reader.Read().Select((x, i) => new CustomAttributeData
                            {
                                AttributeType = x.Ctor.DeclaringType,
                                CtorInfo = x,
                                Index = i,
                            }).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Some entries point at data that doesn't decode into a valid attribute
                        // record, which can fail in many different ways further down the read path.
                        // Skip just this entity instead of aborting the whole load.
                        Console.Error.WriteLine($"[Warning] Failed to read custom attribute data at index {customAttributeIndex} for assembly '{asm.ShortName}': {ex.GetType().Name}: {ex.Message}");
                        attributes = new List<CustomAttributeData>();
                    }

                    asm.Model.AttributesByDataIndices[customAttributeIndex] = attributes;
                }

                foreach (var attribute in attributes)
                    yield return attribute;
            }
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly asm, int token, int customAttributeIndex) =>
                getCustomAttributes(asm, asm.Model.GetCustomAttributeIndex(asm, token, customAttributeIndex)).ToList();
            
        public static IList<CustomAttributeData> GetCustomAttributes(Assembly asm) => GetCustomAttributes(asm, asm.MetadataToken, asm.AssemblyDefinition.CustomAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(EventInfo evt) => GetCustomAttributes(evt.Assembly, evt.MetadataToken, evt.Definition.CustomAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(FieldInfo field) => GetCustomAttributes(field.Assembly, field.MetadataToken, field.Definition.CustomAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(MethodBase method) => GetCustomAttributes(method.Assembly, method.MetadataToken, method.Definition.CustomAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo param) => GetCustomAttributes(param.DeclaringMethod.Assembly, param.MetadataToken, param.Definition.CustomAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(PropertyInfo prop)
            => prop.Definition.IsValid 
                ? GetCustomAttributes(prop.Assembly, prop.MetadataToken, prop.Definition.CustomAttributeIndex) 
                : new List<CustomAttributeData>();
        public static IList<CustomAttributeData> GetCustomAttributes(TypeInfo type) => type.Definition.IsValid
            ? GetCustomAttributes(type.Assembly, type.MetadataToken, type.Definition.CustomAttributeIndex) 
            : new List<CustomAttributeData>();
    }
}
