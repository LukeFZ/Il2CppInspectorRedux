using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct NestedTypeIndex(int value) : IIndexType<NestedTypeIndex>, IReadable, IEquatable<NestedTypeIndex>
{
    public const string TagPrefix = nameof(NestedTypeIndex);

    static string IIndexType<NestedTypeIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<NestedTypeIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<NestedTypeIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(NestedTypeIndex idx) => idx._value;
    public static implicit operator NestedTypeIndex(int idx) => new(idx);

    public static bool operator ==(NestedTypeIndex left, NestedTypeIndex right)
        => left._value == right._value;

    public static bool operator !=(NestedTypeIndex left, NestedTypeIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is NestedTypeIndex other && Equals(other);

    public readonly bool Equals(NestedTypeIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}