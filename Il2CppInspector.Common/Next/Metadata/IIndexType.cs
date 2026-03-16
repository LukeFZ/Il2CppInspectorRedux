using System.Diagnostics;
using System.Runtime.CompilerServices;
using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

file static class Il2CppMetadataIndex
{
    public const int Invalid = -1;
    public const char NumberZeroChar = '0';
}

public interface IIndexType<T> where T 
    : IIndexType<T>, allows ref struct
{
    public static abstract string TagPrefix { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSizeFromTag(in StructVersion version)
    {
        // Fallback to the default for unsupported configurations.
        if (version < MetadataVersions.V380 || version.Tag == null)
        {
            return sizeof(int);
        }

        // Get the position of the size tag in the version tag.
        // Bail out if not found.
        var sizeTagPosition = version.Tag.IndexOf(T.TagPrefix, StringComparison.Ordinal);
        if (sizeTagPosition == -1)
        {
            return sizeof(int);
        }

        // Get the number that follows immediately after the size tag, and convert it to the actual size.
        var numberChar = version.Tag[sizeTagPosition + T.TagPrefix.Length];
        return numberChar - Il2CppMetadataIndex.NumberZeroChar;
    }

    public static int IndexSize(in StructVersion version = default, in ReaderConfig config = default)
        => GetSizeFromTag(version);

    public static int ReadIndex<TReader>(ref Reader<TReader> reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        var size = GetSizeFromTag(in version);

        int index;
        if (size == sizeof(int))
        {
            index = reader.ReadPrimitive<int>();
        }
        else if (size == sizeof(ushort))
        {
            var value = reader.ReadPrimitive<ushort>();
            index = value == ushort.MaxValue ? Il2CppMetadataIndex.Invalid : value;
        }
        else
        {
            Debug.Assert(size == sizeof(byte));

            var value = reader.ReadPrimitive<byte>();
            index = value == byte.MaxValue ? Il2CppMetadataIndex.Invalid : value;
        }

        return index;
    }
}