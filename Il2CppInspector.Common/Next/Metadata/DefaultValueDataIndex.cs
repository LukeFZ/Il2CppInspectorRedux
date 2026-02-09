using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct DefaultValueDataIndex(int value) : IIndexType<DefaultValueDataIndex>, IReadable, IEquatable<DefaultValueDataIndex>
{
    public const string TagPrefix = nameof(DefaultValueDataIndex);

    static string IIndexType<DefaultValueDataIndex>.TagPrefix => TagPrefix;
    static StructVersion IIndexType<DefaultValueDataIndex>.AddedVersion => MetadataVersions.V390;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<DefaultValueDataIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<DefaultValueDataIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(DefaultValueDataIndex idx) => idx._value;
    public static implicit operator DefaultValueDataIndex(int idx) => new(idx);

    public static bool operator ==(DefaultValueDataIndex left, DefaultValueDataIndex right)
        => left._value == right._value;

    public static bool operator !=(DefaultValueDataIndex left, DefaultValueDataIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is DefaultValueDataIndex other && Equals(other);

    public readonly bool Equals(DefaultValueDataIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}