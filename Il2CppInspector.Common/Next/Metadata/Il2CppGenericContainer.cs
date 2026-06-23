using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public record struct Il2CppGenericContainer : IReadable
{
    public int OwnerIndex { get; private set; }
    public int TypeArgc { get; private set; }
    public int IsMethod { get; private set; }
    public GenericParameterIndex GenericParameterStart { get; private set; }

    void IReadable.Read<TReader>(ref Reader<TReader> reader, in StructVersion version)
    {
        OwnerIndex = reader.ReadPrimitive<int>();

        if (version >= MetadataVersions.V1060)
        {
            TypeArgc = reader.ReadPrimitive<ushort>();
            IsMethod = reader.ReadPrimitive<byte>();
        }
        else
        {
            TypeArgc = reader.ReadPrimitive<int>();
            IsMethod = reader.ReadPrimitive<int>();
        }

        GenericParameterStart = reader.ReadVersionedObject<GenericParameterIndex>(version);
    }

    static int IReadable.Size(in StructVersion version, in ReaderConfig config)
    {
        var size = sizeof(int);

        if (version >= MetadataVersions.V1060)
        {
            size += sizeof(ushort);
            size += sizeof(byte);
        }
        else
        {
            size += sizeof(int);
            size += sizeof(int);
        }

        size += GenericParameterIndex.Size(version, config);

        return size;
    }
}