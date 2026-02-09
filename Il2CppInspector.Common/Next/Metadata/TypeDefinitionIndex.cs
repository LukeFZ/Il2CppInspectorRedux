using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct TypeDefinitionIndex(int value) : IIndexType<TypeDefinitionIndex>, IReadable, IEquatable<TypeDefinitionIndex>
{
    public const string TagPrefix = nameof(TypeDefinitionIndex);

    static string IIndexType<TypeDefinitionIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<TypeDefinitionIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<TypeDefinitionIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(TypeDefinitionIndex idx) => idx._value;
    public static implicit operator TypeDefinitionIndex(int idx) => new(idx);

    public static bool operator ==(TypeDefinitionIndex left, TypeDefinitionIndex right)
        => left._value == right._value;

    public static bool operator !=(TypeDefinitionIndex left, TypeDefinitionIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is TypeDefinitionIndex other && Equals(other);

    public readonly bool Equals(TypeDefinitionIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}