using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct PropertyIndex(int value) : IIndexType<PropertyIndex>, IReadable, IEquatable<PropertyIndex>
{
    public const string TagPrefix = nameof(PropertyIndex);

    static string IIndexType<PropertyIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<PropertyIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<PropertyIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(PropertyIndex idx) => idx._value;
    public static implicit operator PropertyIndex(int idx) => new(idx);

    public static bool operator ==(PropertyIndex left, PropertyIndex right)
        => left._value == right._value;

    public static bool operator !=(PropertyIndex left, PropertyIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is PropertyIndex other && Equals(other);

    public readonly bool Equals(PropertyIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}