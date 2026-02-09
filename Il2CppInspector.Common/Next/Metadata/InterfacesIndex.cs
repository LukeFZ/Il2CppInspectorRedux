using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct InterfacesIndex(int value) : IIndexType<InterfacesIndex>, IReadable, IEquatable<InterfacesIndex>
{
    public const string TagPrefix = nameof(InterfacesIndex);

    static string IIndexType<InterfacesIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<InterfacesIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<InterfacesIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(InterfacesIndex idx) => idx._value;
    public static implicit operator InterfacesIndex(int idx) => new(idx);

    public static bool operator ==(InterfacesIndex left, InterfacesIndex right)
        => left._value == right._value;

    public static bool operator !=(InterfacesIndex left, InterfacesIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is InterfacesIndex other && Equals(other);

    public readonly bool Equals(InterfacesIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}