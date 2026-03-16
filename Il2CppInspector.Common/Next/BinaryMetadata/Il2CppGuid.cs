using VersionedSerialization;

namespace Il2CppInspector.Next.BinaryMetadata;

public record struct Il2CppGuid : IReadable
{
    public Guid Value;

    public void Read<TReader>(ref Reader<TReader> reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        var guid = reader.ReadBytes(16);
        Value = new Guid(guid, false);
    }

    public static int Size(in StructVersion version = default, in ReaderConfig config = default)
    {
        return 16;
    }

    public static implicit operator Guid(Il2CppGuid value) => value.Value;
}