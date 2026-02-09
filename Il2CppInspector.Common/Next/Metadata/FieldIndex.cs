using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct FieldIndex(int value) : IIndexType<FieldIndex>, IReadable, IEquatable<FieldIndex>
{
    public const string TagPrefix = nameof(FieldIndex);

    static string IIndexType<FieldIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<FieldIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<FieldIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(FieldIndex idx) => idx._value;
    public static implicit operator FieldIndex(int idx) => new(idx);

    public static bool operator ==(FieldIndex left, FieldIndex right)
        => left._value == right._value;

    public static bool operator !=(FieldIndex left, FieldIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is FieldIndex other && Equals(other);

    public readonly bool Equals(FieldIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}