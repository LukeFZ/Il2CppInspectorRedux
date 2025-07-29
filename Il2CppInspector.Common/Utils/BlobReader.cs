using NoisyCowStudios.Bin2Object;
using System.Text;
using System.Diagnostics;
using Il2CppInspector.Next;
using Il2CppInspector.Next.BinaryMetadata;
using Il2CppInspector.Next.Metadata;
using Spectre.Console;

namespace Il2CppInspector.Utils;

public static class BlobReader
{
    public static object GetConstantValueFromBlob(Il2CppInspector inspector, Il2CppTypeEnum type, BinaryObjectStreamReader blob)
    {
        const byte kArrayTypeWithDifferentElements = 1;

        object value = null;

        switch (type)
        {
            case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                value = blob.ReadBoolean();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_U1:
            case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                value = blob.ReadByte();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                // UTF-8 character assumed
                value = (char)blob.ReadPrimitive<short>();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                value = blob.ReadUInt16();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                value = blob.ReadInt16();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                value = ReadUInt32();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                value = ReadInt32();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                value = blob.ReadUInt64();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                value = blob.ReadInt64();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                value = blob.ReadSingle();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                value = blob.ReadDouble();
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                var uiLen = ReadInt32();
                if (uiLen != -1)
                    value = Encoding.UTF8.GetString(blob.ReadBytes(uiLen));

                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                var length = ReadInt32();
                if (length == -1)
                    break;

                // This is only used in custom arguments.
                // We actually want the reflection TypeInfo here, but as we do not have it yet
                // we store everything in a custom array type to be changed out later in the TypeModel.

                var arrayElementType = ReadEncodedTypeEnum(inspector, blob, out var arrayElementDef);
                var arrayElementsAreDifferent = blob.ReadByte();

                var array = new ConstantBlobArrayElement[length];
                if (arrayElementsAreDifferent == kArrayTypeWithDifferentElements)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var elementType = ReadEncodedTypeEnum(inspector, blob, out var elementTypeDef);
                        array[i] = new ConstantBlobArrayElement(elementTypeDef, GetConstantValueFromBlob(inspector, elementType, blob), elementType);
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        array[i] = new ConstantBlobArrayElement(arrayElementDef, GetConstantValueFromBlob(inspector, arrayElementType, blob), arrayElementType);
                    }
                }

                value = new ConstantBlobArray(arrayElementDef, array, arrayElementType);

                break;

            case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
            case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
            case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_IL2CPP_TYPE_INDEX:
                var index = blob.ReadCompressedInt32();
                if (index != -1)
                    value = inspector.TypeReferences[index];

                break;
            case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                break;
            default:
                Debugger.Break();
                break;
        }

        return value;

        int ReadInt32()
        {
            if (blob.Version >= MetadataVersions.V290)
            {
                var address = blob.Position;

                try
                {
                    return blob.ReadCompressedInt32();
                }
                catch (InvalidDataException)
                {
                    AnsiConsole.WriteLine($"Found invalid compressed int at metadata address 0x{address:x8}. Reading as normal int.");
                    return blob.ReadInt32(address);
                }
            }

            return blob.ReadInt32();
        }

        uint ReadUInt32()
        {
            if (blob.Version >= MetadataVersions.V290)
            {
                var address = blob.Position;

                try
                {
                    return blob.ReadCompressedUInt32();
                }
                catch (InvalidDataException)
                {
                    AnsiConsole.WriteLine($"Found invalid compressed uint at metadata address 0x{address:x8}. Reading as normal uint.");
                    return blob.ReadUInt32(address);
                }
            }

            return blob.ReadUInt32();
        }
    }

    public static Il2CppTypeEnum ReadEncodedTypeEnum(Il2CppInspector inspector, BinaryObjectStream blob,
        out Il2CppTypeDefinition enumType)
    {
        enumType = default;

        var typeEnum = (Il2CppTypeEnum)blob.ReadByte();
        if (typeEnum == Il2CppTypeEnum.IL2CPP_TYPE_ENUM)
        {
            var typeIndex = blob.ReadCompressedInt32();
            var typeHandle = inspector.TypeReferences[typeIndex].Data.KlassIndex;
            enumType = inspector.TypeDefinitions[typeHandle];

            var elementTypeIndex = enumType.GetEnumElementTypeIndex(inspector.Version);

            var elementTypeHandle = inspector.TypeReferences[elementTypeIndex].Data.KlassIndex;
            var elementType = inspector.TypeDefinitions[elementTypeHandle];
            typeEnum = inspector.TypeReferences[elementType.ByValTypeIndex].Type;
        }
        // This technically also handles SZARRAY (System.Array) and all others by just returning their system type

        return typeEnum;
    }

    public record ConstantBlobArray(Il2CppTypeDefinition ArrayTypeDef, ConstantBlobArrayElement[] Elements, Il2CppTypeEnum ArrayTypeEnum);

    public record ConstantBlobArrayElement(Il2CppTypeDefinition TypeDef, object Value, Il2CppTypeEnum TypeEnum);
}