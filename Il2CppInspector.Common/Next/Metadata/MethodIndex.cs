using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct MethodIndex(int value) : IIndexType<MethodIndex>, IReadable, IEquatable<MethodIndex>
{
    public const string TagPrefix = nameof(MethodIndex);

    static string IIndexType<MethodIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<MethodIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<MethodIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(MethodIndex idx) => idx._value;
    public static implicit operator MethodIndex(int idx) => new(idx);

    public static bool operator ==(MethodIndex left, MethodIndex right)
        => left._value == right._value;

    public static bool operator !=(MethodIndex left, MethodIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is MethodIndex other && Equals(other);

    public readonly bool Equals(MethodIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}