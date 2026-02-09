using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

public struct EventIndex(int value) : IIndexType<EventIndex>, IReadable, IEquatable<EventIndex>
{
    public const string TagPrefix = nameof(EventIndex);

    static string IIndexType<EventIndex>.TagPrefix => TagPrefix;

    private int _value = value;

    public static int Size(in StructVersion version = default, bool is32Bit = false)
        => IIndexType<EventIndex>.IndexSize(version, is32Bit);

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = IIndexType<EventIndex>.ReadIndex(ref reader, in version);
    }

    #region Operators + ToString

    public static implicit operator int(EventIndex idx) => idx._value;
    public static implicit operator EventIndex(int idx) => new(idx);

    public static bool operator ==(EventIndex left, EventIndex right)
        => left._value == right._value;

    public static bool operator !=(EventIndex left, EventIndex right)
        => !(left == right);

    public readonly override bool Equals(object obj)
        => obj is EventIndex other && Equals(other);

    public readonly bool Equals(EventIndex other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => _value.ToString();

    #endregion
}