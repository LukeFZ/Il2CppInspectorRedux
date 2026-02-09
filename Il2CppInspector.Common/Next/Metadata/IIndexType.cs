using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public interface IIndexType<T> where T 
    : IIndexType<T>, allows ref struct
{
    public static abstract string TagPrefix { get; }

    private static string TagSize4 => $"{T.TagPrefix}4";
    private static string TagSize2 => $"{T.TagPrefix}2";
    private static string TagSize1 => $"{T.TagPrefix}1";

    private static bool HasCustomSize(in StructVersion version)
        => version.Tag != null
           && (version.Tag.Contains(TagSize2)
               || version.Tag.Contains(TagSize1));

    public static int IndexSize(in StructVersion version = default, bool is32Bit = false)
    {
        if (version.Tag != null && HasCustomSize(version))
        {
            if (version.Tag.Contains(TagSize2))
                return sizeof(ushort);

            if (version.Tag.Contains(TagSize1))
                return sizeof(byte);
        }

        return sizeof(int);

    }

    public static int ReadIndex<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        if (version.Tag != null && HasCustomSize(version))
        {
            if (version.Tag.Contains(TagSize2))
            {
                var value = reader.ReadPrimitive<ushort>(); 
                return value == ushort.MaxValue ? -1 : value;
            }

            if (version.Tag.Contains(TagSize1))
            {
                var value = reader.ReadPrimitive<byte>();
                return value == byte.MaxValue ? -1 : value;
            }
        }

        return reader.ReadPrimitive<int>();
    }
}