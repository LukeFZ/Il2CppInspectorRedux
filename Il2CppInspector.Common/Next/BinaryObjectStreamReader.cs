using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NoisyCowStudios.Bin2Object;
using VersionedSerialization;
using VersionedSerialization.Impl;

namespace Il2CppInspector.Next;

public class BinaryObjectStreamReader : BinaryObjectStream, ISeekableReader
{
    public new StructVersion Version
    {
        get;
        set
        {
            field = value;
            base.Version = field.AsDouble;
        }
    }

    public virtual int Bits { get; set; }
    public bool Is32Bit => Bits == 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TTo Cast<TFrom, TTo>(in TFrom from) => Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in from));

    public T ReadPrimitive<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(sbyte))
            return Cast<byte, T>(ReadByte());

        if (typeof(T) == typeof(short))
            return Cast<short, T>(ReadInt16());

        if (typeof(T) == typeof(int))
            return Cast<int, T>(ReadInt32());

        if (typeof(T) == typeof(long))
            return Cast<long, T>(ReadInt64());

        if (typeof(T) == typeof(byte))
            return Cast<byte, T>(ReadByte());

        if (typeof(T) == typeof(ushort))
            return Cast<ushort, T>(ReadUInt16());

        if (typeof(T) == typeof(uint))
            return Cast<uint, T>(ReadUInt32());

        if (typeof(T) == typeof(ulong))
            return Cast<ulong, T>(ReadUInt64());

        Debug.Assert(false, "Invalid primitive type");
        throw new InvalidOperationException();
    }

    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
    {
        for (int i = 0; i < dest.Length; i++)
        {
            dest[i] = ReadPrimitive<T>();
        }
    }

    public void Read<T>(scoped Span<T> dest) where T : unmanaged
    {
        var asBytes = MemoryMarshal.Cast<T, byte>(dest);
        Reader.ReadBytes(asBytes.Length).AsSpan().CopyTo(asBytes);
    }

    public string ReadString(int length = -1, Encoding encoding = null)
    {
        return length == -1 
            ? ReadNullTerminatedString(encoding) 
            : ReadFixedLengthString(length, encoding);
    }

    ReadOnlySpan<byte> IReader.ReadBytes(long length)
    {
        return ReadBytes(checked((int)length));
    }

    // Reader-wrapping helper methods

    int ISeekableReader.Offset
    {
        get => checked((int)Position);
        set => Position = value;
    }

    int ISeekableReader.Length => checked((int)Length);

    public ulong ReadNativeUInt()
    {
        return Endianness == Endianness.Little
            ? new Reader<LittleEndianSeekableReader<BinaryObjectStreamReader>>(
                new LittleEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadNativeUInt()
            : new Reader<BigEndianSeekableReader<BinaryObjectStreamReader>>(
                new BigEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadNativeUInt();
    }

    public T ReadVersionedObject<T>() where T : IReadable, new()
    {
        return Endianness == Endianness.Little
            ? new Reader<LittleEndianSeekableReader<BinaryObjectStreamReader>>(
                new LittleEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadVersionedObject<T>(Version)
            : new Reader<BigEndianSeekableReader<BinaryObjectStreamReader>>(
                new BigEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadVersionedObject<T>(Version);
    }

    public ImmutableArray<T> ReadVersionedObjectArray<T>(long count) where T : IReadable, new()
    {
        return Endianness == Endianness.Little
            ? new Reader<LittleEndianSeekableReader<BinaryObjectStreamReader>>(
                new LittleEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadVersionedObjectArray<T>(count, Version)
            : new Reader<BigEndianSeekableReader<BinaryObjectStreamReader>>(
                new BigEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadVersionedObjectArray<T>(count, Version);
    }

    public ImmutableArray<T> ReadPrimitiveArray<T>(long count) where T : unmanaged
    {
        return Endianness == Endianness.Little
            ? new Reader<LittleEndianSeekableReader<BinaryObjectStreamReader>>(
                new LittleEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadPrimitiveArray<T>(count)
            : new Reader<BigEndianSeekableReader<BinaryObjectStreamReader>>(
                new BigEndianSeekableReader<BinaryObjectStreamReader>(this), new ReaderConfig(Is32Bit)).ReadPrimitiveArray<T>(count);
    }

    public TType ReadPrimitive<TType>(long addr) where TType : unmanaged
    {
        Position = addr;
        return ReadPrimitive<TType>();
    }

    public ImmutableArray<TType> ReadPrimitiveArray<TType>(long addr, long count) where TType : unmanaged
    {
        Position = addr;
        return ReadPrimitiveArray<TType>(count);
    }

    public TType ReadVersionedObject<TType>(long addr) where TType : IReadable, new()
    {
        Position = addr;
        return ReadVersionedObject<TType>();
    }

    public ImmutableArray<TType> ReadVersionedObjectArray<TType>(long addr, long count)
        where TType : IReadable, new()
    {
        Position = addr;
        return ReadVersionedObjectArray<TType>(count);
    }
}