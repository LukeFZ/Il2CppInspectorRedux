using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace VersionedSerialization.Impl;

file static class EndianReader
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> src)
    {
        Debug.Assert(Unsafe.SizeOf<TFrom>() == Unsafe.SizeOf<TTo>());
        return Unsafe.BitCast<Span<TFrom>, Span<TTo>>(src);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    private static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> src)
    {
        Debug.Assert(Unsafe.SizeOf<TFrom>() == Unsafe.SizeOf<TTo>());
        return Unsafe.BitCast<ReadOnlySpan<TFrom>, ReadOnlySpan<TTo>>(src);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseEndianness<T>(ReadOnlySpan<T> input, Span<T> output)
        where T : unmanaged
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException();

        if (Unsafe.SizeOf<T>() == sizeof(short))
        {
            var src = Cast<T, short>(input);
            var dst = Cast<T, short>(output);
            BinaryPrimitives.ReverseEndianness(src, dst);
        }
        else if (Unsafe.SizeOf<T>() == sizeof(int))
        {
            var src = Cast<T, int>(input);
            var dst = Cast<T, int>(output);
            BinaryPrimitives.ReverseEndianness(src, dst);
        }
        else if (Unsafe.SizeOf<T>() == sizeof(long))
        {
            var src = Cast<T, long>(input);
            var dst = Cast<T, long>(output);
            BinaryPrimitives.ReverseEndianness(src, dst);
        }
        else if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<Int128>())
        {
            var src = Cast<T, Int128>(input);
            var dst = Cast<T, Int128>(output);
            BinaryPrimitives.ReverseEndianness(src, dst);
        }

        Debug.Assert(false, "Failed to reverse endianness for type");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLittleEndian<TReader, TType>(ref TReader reader, scoped Span<TType> dest)
        where TReader : IReader, allows ref struct
        where TType : unmanaged
    {
        if (BitConverter.IsLittleEndian || Unsafe.SizeOf<TType>() == sizeof(byte))
        {
            reader.ReadPrimitive(dest);
        }
        else
        {
            var data = reader.ReadBytes(Unsafe.SizeOf<TType>() * dest.Length);
            var src = MemoryMarshal.Cast<byte, TType>(data);
            ReverseEndianness(src, dest);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadBigEndian<TReader, TType>(ref TReader reader, scoped Span<TType> dest)
        where TReader : IReader, allows ref struct
        where TType : unmanaged
    {
        if (!BitConverter.IsLittleEndian || Unsafe.SizeOf<TType>() == sizeof(byte))
        {
            reader.ReadPrimitive(dest);
        }
        else
        {
            var data = reader.ReadBytes(Unsafe.SizeOf<TType>() * dest.Length);
            var src = MemoryMarshal.Cast<byte, TType>(data);
            ReverseEndianness(src, dest);
        }
    }
}

public ref struct LittleEndianReader<TReader>(TReader impl) : IReader
    where TReader : IReader, allows ref struct
{
    private TReader _impl = impl;

    public string ReadString(int length = -1, Encoding? encoding = null)
        => _impl.ReadString(length, encoding);

    public ReadOnlySpan<byte> ReadBytes(long length)
        => _impl.ReadBytes(length);

    public void Read<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.Read(dest);

    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => EndianReader.ReadLittleEndian(ref _impl, dest);
}

public ref struct LittleEndianSeekableReader<TReader>(TReader impl) : ISeekableReader
    where TReader : ISeekableReader, allows ref struct
{
    private TReader _impl = impl;

    public int Offset
    {
        get => _impl.Offset;
        set => _impl.Offset = value;
    }

    public int Length => _impl.Length;

    public string ReadString(int length = -1, Encoding? encoding = null)
        => _impl.ReadString(length, encoding);

    public ReadOnlySpan<byte> ReadBytes(long length)
        => _impl.ReadBytes(length);

    public void Read<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.Read(dest);

    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => EndianReader.ReadLittleEndian(ref _impl, dest);
}

public ref struct BigEndianReader<TReader>(TReader impl) : IReader
    where TReader : IReader, allows ref struct
{
    private TReader _impl = impl;

    public string ReadString(int length = -1, Encoding? encoding = null)
        => _impl.ReadString(length, encoding);

    public ReadOnlySpan<byte> ReadBytes(long length)
        => _impl.ReadBytes(length);

    public void Read<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.Read(dest);

    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => EndianReader.ReadBigEndian(ref _impl, dest);
}

public ref struct BigEndianSeekableReader<TReader>(TReader impl) : ISeekableReader
    where TReader : ISeekableReader, allows ref struct
{
    private TReader _impl = impl;

    public int Offset
    {
        get => _impl.Offset;
        set => _impl.Offset = value;
    }

    public int Length => _impl.Length;

    public string ReadString(int length = -1, Encoding? encoding = null)
        => _impl.ReadString(length, encoding);

    public ReadOnlySpan<byte> ReadBytes(long length)
        => _impl.ReadBytes(length);

    public void Read<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.Read(dest);

    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => EndianReader.ReadBigEndian(ref _impl, dest);
}