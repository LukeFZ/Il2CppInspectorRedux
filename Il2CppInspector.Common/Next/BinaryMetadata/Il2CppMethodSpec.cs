using VersionedSerialization;

namespace Il2CppInspector.Next.BinaryMetadata;

public struct Il2CppMethodSpec : IReadable
{
    public MethodIndex MethodDefinitionIndex;
    public GenericInstIndex ClassIndexIndex;
    public GenericInstIndex MethodIndexIndex;

    void IReadable.Read<TReader>(ref Reader<TReader> reader, in StructVersion version)
    {
        if (version >= MetadataVersions.V1080)
        {
            MethodDefinitionIndex = reader.ReadVersionedObject<MethodIndex>(version);
            ClassIndexIndex = reader.ReadVersionedObject<GenericInstIndex>(version);
            MethodIndexIndex = reader.ReadVersionedObject<GenericInstIndex>(version);
        }
        else
        {
            MethodDefinitionIndex = reader.ReadPrimitive<int>();
            ClassIndexIndex = reader.ReadPrimitive<int>();
            MethodIndexIndex = reader.ReadPrimitive<int>();
        }
    }

    static int IReadable.Size(in StructVersion version, in ReaderConfig config)
    {
        if (version >= MetadataVersions.V1080)
        {
            return MethodIndex.StructSize(version, config)
                   + GenericInstIndex.StructSize(version, config)
                   + GenericInstIndex.StructSize(version, config);
        }

        return sizeof(uint) + sizeof(uint) + sizeof(uint);
    }
}