using Il2CppInspector.Next.Metadata;
using System.Reflection;
using VersionedSerialization;

namespace Il2CppInspector.Next.BinaryMetadata;

using TypeDefinitionIndex = int;
using GenericParameterIndex = int;
using Il2CppMetadataTypeHandle = Pointer<Il2CppTypeDefinition>;
using Il2CppMetadataGenericParameterHandle = Pointer<Il2CppGenericParameter>;

public record struct Il2CppType : IReadable
{
    public record struct DataUnion : IReadable
    {
        public ulong Value;

        public readonly TypeDefinitionIndex KlassIndex => (int)Value;
        public readonly Il2CppMetadataTypeHandle TypeHandle => Value;
        public readonly Pointer<Il2CppType> Type => Value;
        public readonly Pointer<Il2CppArrayType> ArrayType => Value;
        public readonly GenericParameterIndex GenericParameterIndex => (int)Value;
        public readonly Il2CppMetadataGenericParameterHandle GenericParameterHandle => Value;
        public readonly Pointer<Il2CppGenericClass> GenericClass => Value;

        public void Read<TReader>(ref Reader<TReader> reader, in StructVersion version = default) where TReader : IReader, allows ref struct
        {
            Value = reader.ReadNativeUInt();
        }

        static int IReadable.Size(in StructVersion version, in ReaderConfig config)
        {
            return config.Is32Bit ? 4 : 8;
        }
    }

    public DataUnion Data;
    public uint Value;

    public TypeAttributes Attrs 
    { 
        readonly get => (TypeAttributes)(Value & 0xFFFF); 
        set => Value = (Value & 0xFFFF0000) | (uint)value;
    }

    public Il2CppTypeEnum Type
    {
        readonly get => (Il2CppTypeEnum)((Value >> 16) & 0b11111111);
        set => Value = (Value & 0xFF00FFFF) | ((uint)value) << 16;
    }

    public uint NumModifiers
    {
        readonly get => (Value >> 24) & 0b11111;
        set => Value = (Value & 0xE0FFFFFF) | value << 24;
    }

    public bool ByRef
    {
        readonly get => ((Value >> 29) & 1) == 1;
        set => Value = (Value & 0xDFFFFFFF) | (value ? 1u : 0u) << 29;
    }

    public bool Pinned
    {
        readonly get => ((Value >> 30) & 1) == 1;
        set => Value = (Value & 0xBFFFFFFF) | (value ? 1u : 0u) << 30;
    }

    public bool ValueType
    {
        readonly get => ((Value >> 31) & 1) == 1;
        set => Value = (Value & 0x7FFFFFFF) | (value ? 1u : 0u) << 31;
    }

    public void Read<TReader>(ref Reader<TReader> reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        Data.Read(ref reader, version);
        Value = reader.ReadPrimitive<uint>();

        if (MetadataVersions.V272 > version)
        {
            // Versions pre-27.2 had NumModifiers at 6 bits and no ValueType bit
            var numModifiers = (Value >> 24) & 0b111111;

            // If NumModifiers > 31, we throw here (as the old behavior isn't implemented
            if (numModifiers > 31)
                throw new InvalidOperationException(
                    "Versions pre-27.2 with a type having more than 31 modifiers are not supported yet");

            // Else, we do some bit-juggling to convert the old value into the new format:
            Value =
                (Value & 0xFFFFFF) | // Attributes + Type
                (((Value >> 24) & 0b111111) << 24) | // 5 Bits for the modifiers 
                (((Value >> 30) & 1) << 29) | // Shifted ByRef
                (((Value >> 31) & 1) << 30) | // Shifted Pinned
                0; // 0 ValueType
        }
    }

    public static int Size(in StructVersion version = default, in ReaderConfig config = default)
    {
        return DataUnion.StructSize(version, config) + sizeof(uint);
    }

    public static Il2CppType FromTypeEnum(Il2CppTypeEnum type)
        => new()
        {
            Value = (uint)type << 16
        };
}