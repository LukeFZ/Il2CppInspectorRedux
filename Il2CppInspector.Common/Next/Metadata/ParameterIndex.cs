using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct ParameterIndex(int value) : IIndexType<ParameterIndex>, IReadable, IEquatable<ParameterIndex>
{
    public const string TagPrefix = nameof(ParameterIndex);

    static string IIndexType<ParameterIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<ParameterIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<ParameterIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(ParameterIndex idx) => idx._value;
    public static implicit operator ParameterIndex(int idx) => new(idx);

    public static bool operator ==(ParameterIndex left, ParameterIndex right)
        => left._value == right._value;

    public static bool operator !=(ParameterIndex left, ParameterIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is ParameterIndex other && Equals(other);

    public readonly bool Equals(ParameterIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}