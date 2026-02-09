using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct GenericParameterIndex(int value) : IIndexType<GenericParameterIndex>, IReadable, IEquatable<GenericParameterIndex>
{
    public const string TagPrefix = nameof(GenericParameterIndex);

    static string IIndexType<GenericParameterIndex>.TagPrefix => TagPrefix;
    static StructVersion IIndexType<GenericParameterIndex>.AddedVersion => MetadataVersions.V390;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<GenericParameterIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<GenericParameterIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(GenericParameterIndex idx) => idx._value;
    public static implicit operator GenericParameterIndex(int idx) => new(idx);

    public static bool operator ==(GenericParameterIndex left, GenericParameterIndex right)
        => left._value == right._value;

    public static bool operator !=(GenericParameterIndex left, GenericParameterIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is GenericParameterIndex other && Equals(other);

    public readonly bool Equals(GenericParameterIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}