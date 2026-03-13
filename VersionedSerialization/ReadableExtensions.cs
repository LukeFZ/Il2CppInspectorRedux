namespace VersionedSerialization;

public static class ReadableExtensions
{
    extension<TReadable>(TReadable)
        where TReadable : IReadable, new()
    {
        public static int StructSize(in StructVersion version = default, in ReaderConfig config = default)
            => TReadable.Size(version, config);

        public static TReadable FromBytes(ReadOnlySpan<byte> data, bool littleEndian = true, ReaderConfig config = default,
            in StructVersion version = default)
        {
            if (littleEndian)
            {
                var reader = Reader.LittleEndian(data, config: config);
                return reader.ReadVersionedObject<TReadable>(version);
            }
            else
            {
                var reader = Reader.BigEndian(data, config: config);
                return reader.ReadVersionedObject<TReadable>(version);
            }
        }
    }
}