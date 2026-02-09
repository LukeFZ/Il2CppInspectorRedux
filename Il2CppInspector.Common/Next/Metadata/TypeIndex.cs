using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct TypeIndex(int value) : IIndexType<TypeIndex>, IReadable, IEquatable<TypeIndex>
{
    public const string TagPrefix = nameof(TypeIndex);

    static string IIndexType<TypeIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<TypeIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<TypeIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(TypeIndex idx) => idx._value;
    public static implicit operator TypeIndex(int idx) => new(idx);

    public static bool operator ==(TypeIndex left, TypeIndex right)
        => left._value == right._value;

    public static bool operator !=(TypeIndex left, TypeIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is TypeIndex other && Equals(other);

    public readonly bool Equals(TypeIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}