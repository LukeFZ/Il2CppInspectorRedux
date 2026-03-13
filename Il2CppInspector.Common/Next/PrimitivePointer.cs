#nullable enable
using System.Collections.Immutable;
using VersionedSerialization;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next;

public struct PrimitivePointer<T>(ulong value = 0) : IReadable, IEquatable<PrimitivePointer<T>> where T : unmanaged
{
    [NativeInteger]
    private ulong _value = value;

    public readonly ulong PointerValue => _value;
    public readonly bool Null => _value == 0;

    void IReadable.Read<TReader>(ref Reader<TReader> reader, in StructVersion version)
    {
        _value = reader.ReadNativeUInt();
    }

    public static int Size(in StructVersion version = default, in ReaderConfig config = default)
    {
        return config.Is32Bit ? sizeof(uint) : sizeof(ulong);
    }

    public readonly T Read<TReader>(ref Reader<TReader> reader)
        where TReader : ISeekableReader, allows ref struct
    {
        reader.Offset = (int)PointerValue;
        return reader.ReadPrimitive<T>();
    }

    public readonly ImmutableArray<T> ReadArray<TReader>(ref Reader<TReader> reader, long count)
        where TReader : ISeekableReader, allows ref struct
    {
        reader.Offset = (int)PointerValue;
        return reader.ReadPrimitiveArray<T>(count);
    }

    public static implicit operator PrimitivePointer<T>(ulong value) => new(value);
    public static implicit operator ulong(PrimitivePointer<T> ptr) => ptr.PointerValue;

    #region Equality operators + ToString

    public static bool operator ==(PrimitivePointer<T> left, PrimitivePointer<T> right)
        => left._value == right._value;

    public static bool operator !=(PrimitivePointer<T> left, PrimitivePointer<T> right)
        => !(left == right);

    public readonly override bool Equals(object? obj)
        => obj is PrimitivePointer<T> other && Equals(other);

    public readonly bool Equals(PrimitivePointer<T> other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => $"0x{_value:X} <{typeof(T).Name}>";

    #endregion
}