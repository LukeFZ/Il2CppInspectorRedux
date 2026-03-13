using System.Collections.Immutable;
using VersionedSerialization.Impl;

namespace VersionedSerialization;

public static class Reader
{
    public static Reader<T> From<T>(T reader, ReaderConfig config = default) where T : IReader, allows ref struct
        => new(reader, config);

    public static Reader<LittleEndianSeekableReader<SpanReader>> LittleEndian(ReadOnlySpan<byte> data, int offset = 0,
        ReaderConfig config = default)
        => new(new SpanReader(data, offset).AsLittleEndian(), config);

    public static Reader<BigEndianSeekableReader<SpanReader>> BigEndian(ReadOnlySpan<byte> data, int offset = 0,
        ReaderConfig config = default)
        => new(new SpanReader(data, offset).AsBigEndian(), config);

    public static T Read<T>(ReadOnlySpan<byte> data, int offset = 0, bool littleEndian = true,
        ReaderConfig config = default)
        where T : unmanaged
    {
        if (littleEndian)
        {
            var reader = LittleEndian(data, offset, config);
            return reader.Read<T>();
        }
        else
        {
            var reader = BigEndian(data, offset, config);
            return reader.Read<T>();
        }
    }

    public static T ReadPrimitive<T>(ReadOnlySpan<byte> data, int offset = 0, bool littleEndian = true,
        ReaderConfig config = default)
        where T : unmanaged
    {
        if (littleEndian)
        {
            var reader = LittleEndian(data, offset, config);
            return reader.ReadPrimitive<T>();
        }
        else
        {
            var reader = BigEndian(data, offset, config);
            return reader.ReadPrimitive<T>();
        }
    }

    public static ImmutableArray<T> ReadPrimitiveArray<T>(ReadOnlySpan<byte> data, long count, int offset = 0, bool littleEndian = true,
        ReaderConfig config = default)
        where T : unmanaged
    {
        if (littleEndian)
        {
            var reader = LittleEndian(data, offset, config);
            return reader.ReadPrimitiveArray<T>(count);
        }
        else
        {
            var reader = BigEndian(data, offset, config);
            return reader.ReadPrimitiveArray<T>(count);
        }
    }

    public static T ReadVersionedObject<T>(ReadOnlySpan<byte> data, StructVersion version = default, int offset = 0, bool littleEndian = true,
        ReaderConfig config = default)
        where T : IReadable, new()
    {
        if (littleEndian)
        {
            var reader = LittleEndian(data, offset, config);
            return reader.ReadVersionedObject<T>(version);
        }
        else
        {
            var reader = BigEndian(data, offset, config);
            return reader.ReadVersionedObject<T>(version);
        }
    }

    public static ImmutableArray<T> ReadVersionedObjectArray<T>(ReadOnlySpan<byte> data, long count,
        StructVersion version = default, int offset = 0, bool littleEndian = true,
        ReaderConfig config = default)
        where T : IReadable, new()
    {
        if (littleEndian)
        {
            var reader = LittleEndian(data, offset, config);
            return reader.ReadVersionedObjectArray<T>(count, version);
        }
        else
        {
            var reader = BigEndian(data, offset, config);
            return reader.ReadVersionedObjectArray<T>(count, version);
        }
    }
}