using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using Il2CppInspector.Next;
using Il2CppInspector.Next.BinaryMetadata;
using Il2CppInspector.Next.Metadata;
using Il2CppInspector.Reflection;
using Il2CppInspector.Utils;

namespace Il2CppInspector
{
    public class CustomAttributeDataReader
    {
        private readonly Il2CppInspector _inspector;
        private readonly Assembly _assembly;
        private readonly BinaryObjectStreamReader _data;

        private readonly uint _start;
        private readonly uint _end;

        private readonly long _ctorBufferStart;
        private readonly long _dataBufferStart;

        public uint Count { get; }

        public CustomAttributeDataReader(Il2CppInspector inspector, Assembly assembly, BinaryObjectStreamReader data, uint startOffset, uint endOffset)
        {
            _inspector = inspector;
            _assembly = assembly;
            _data = data;

            _start = startOffset;
            _end = endOffset;

            data.Position = _start;
            Count = data.ReadCompressedUInt32();

            _ctorBufferStart = data.Position;
            _dataBufferStart = _ctorBufferStart + Count * sizeof(int);
        }

        public IEnumerable<CustomAttributeCtor> Read()
        {
            _data.Position = _ctorBufferStart;

            var ctors = new CustomAttributeCtor[Count];
            for (int i = 0; i < Count; i++)
            {
                ctors[i] = new CustomAttributeCtor();

                var ctorIndex = _data.ReadUInt32();
                if (_assembly.Model.Package.Version < MetadataVersions.V1040 && ctorIndex >= _assembly.Model.MethodsByDefinitionIndex.Length)
                    throw new IndexOutOfRangeException($"Custom attribute ctor index {ctorIndex} is out of range (max {_assembly.Model.MethodsByDefinitionIndex.Length - 1})");
                ctors[i].Ctor = _assembly.Model.Package.Version >= MetadataVersions.V1040
                    ? _assembly.Model.GetMetadataUsageMethod(MetadataUsage.FromEncodedIndex(_assembly.Model.Package, ctorIndex))
                    : _assembly.Model.MethodsByDefinitionIndex[ctorIndex];
            }

            _data.Position = _dataBufferStart;
            for (int i = 0; i < Count; i++)
            {
                var ctor = ctors[i];
                var attrClass = ctor.Ctor.DeclaringType;

                var argumentCount = _data.ReadCompressedUInt32();
                var fieldCount = _data.ReadCompressedUInt32();
                var propertyCount = _data.ReadCompressedUInt32();

                ctor.Arguments = new CustomAttributeArgument[argumentCount];
                for (int j = 0; j < argumentCount; j++)
                {
                    ctor.Arguments[j] = new CustomAttributeArgument();

                    ReadAttributeDataValue(ctor.Arguments[j]);
                }

                ctor.Fields = new CustomAttributeFieldArgument[fieldCount];
                for (int j = 0; j < fieldCount; j++)
                {
                    ctor.Fields[j] = new CustomAttributeFieldArgument();
                    ReadAttributeDataValue(ctor.Fields[j]);

                    var (fieldClass, fieldIndex) = ReadCustomAttributeNamedArgumentClassAndIndex(attrClass);
                    ctor.Fields[j].Field = fieldClass.DeclaredFields[fieldIndex];
                }

                ctor.Properties = new CustomAttributePropertyArgument[propertyCount];
                for (int j = 0; j < propertyCount; j++)
                {
                    ctor.Properties[j] = new CustomAttributePropertyArgument();
                    ReadAttributeDataValue(ctor.Properties[j]);

                    var (propertyClass, propertyIndex) = ReadCustomAttributeNamedArgumentClassAndIndex(attrClass);
                    ctor.Properties[j].Property = propertyClass.DeclaredProperties[propertyIndex];
                }

                yield return ctor;
            }

            if (_data.Position != _end)
                Debugger.Break();
        }

        private void ReadAttributeDataValue(CustomAttributeArgument arg)
        {
            var type = BlobReader.ReadEncodedTypeEnum(_inspector, _data, out var typeDef);
            var value = BlobReader.GetConstantValueFromBlob(_inspector, type, _data);

            value = ConvertAttributeValue(value);

            if (value is CustomAttributeArgument valueAttr)
            {
                arg.Type = valueAttr.Type;
                arg.Value = valueAttr.Value;
            }
            else
            {
                arg.Type = ConvertTypeDef(typeDef, type);
                arg.Value = value;
            }
        }

        private object ConvertAttributeValue(object value)
        {
            switch (value)
            {
                case Il2CppType type:
                    return _assembly.Model.TypesByReferenceIndex[_inspector.TypeReferences.IndexOf(type)];
                case BlobReader.ConstantBlobArray blobArray:
                {
                    var arrValue = new CustomAttributeArgument
                    {
                        Type = ConvertTypeDef(blobArray.ArrayTypeDef, blobArray.ArrayTypeEnum),
                        Value = blobArray.Elements.Select(blobElem => new CustomAttributeArgument
                        {
                            Type = ConvertTypeDef(blobElem.TypeDef, blobElem.TypeEnum),
                            Value = ConvertAttributeValue(blobElem.Value)
                        }).ToArray()
                    };

                    return arrValue;
                }
                default:
                    return value;
            }
        }

        private TypeInfo ConvertTypeDef(Il2CppTypeDefinition typeDef, Il2CppTypeEnum type)
            => typeDef.IsValid
                ? _assembly.Model.TypesByDefinitionIndex[_inspector.TypeDefinitions.IndexOf(typeDef)]
                : _assembly.Model.GetTypeDefinitionFromTypeEnum(type);

        private (TypeInfo, int) ReadCustomAttributeNamedArgumentClassAndIndex(TypeInfo attrInfo)
        {
            var memberIndex = _data.ReadCompressedInt32();
            if (memberIndex >= 0) // Negative indices mean that it's a member of a base class
                return (attrInfo, memberIndex);

            memberIndex = -(memberIndex + 1);

            var typeDefIndex = _data.ReadCompressedUInt32();
            var typeInfo = _assembly.Model.Package.Version >= MetadataVersions.V1040 
                ? _assembly.Model.TypesByReferenceIndex[typeDefIndex] 
                : _assembly.Model.TypesByDefinitionIndex[typeDefIndex];

            return (typeInfo, memberIndex);
        }
    }

    public class CustomAttributeCtor
    {
        public MethodBase Ctor { get; set; }
        public CustomAttributeArgument[] Arguments { get; set; }
        public CustomAttributeFieldArgument[] Fields { get; set; }
        public CustomAttributePropertyArgument[] Properties { get; set; }
    }

    public class CustomAttributeArgument
    {
        public TypeInfo Type { get; set; }
        public object Value { get; set; }
    }

    public class CustomAttributeFieldArgument : CustomAttributeArgument
    {
        public FieldInfo Field { get; set; }
    }

    public class CustomAttributePropertyArgument : CustomAttributeArgument
    {
        public PropertyInfo Property { get; set; }
    }
}