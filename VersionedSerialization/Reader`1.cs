using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace VersionedSerialization;

public ref struct Reader<TReader>(TReader impl, ReaderConfig config = default)
    where TReader : IReader, allows ref struct
{
    private TReader _impl = impl;
    public ReaderConfig Config { get; } = config;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(int length = -1, Encoding? encoding = null)
        => _impl.ReadString(length, encoding);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(long length)
        => _impl.ReadBytes(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>() where T : unmanaged
    {
        Unsafe.SkipInit(out T value);
        Read(ref value);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(scoped ref T value) where T : unmanaged
        => Read(new Span<T>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.Read(dest);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadPrimitive<T>() where T : unmanaged
    {
        Unsafe.SkipInit(out T value);
        ReadPrimitive(ref value);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadPrimitive<T>(scoped ref T value) where T : unmanaged
        => ReadPrimitive(new Span<T>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => _impl.ReadPrimitive(dest);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> ReadPrimitiveArray<T>(long count) where T : unmanaged
    {
        var data = GC.AllocateUninitializedArray<T>((int)count);
        ReadPrimitive(data);
        return ImmutableCollectionsMarshal.AsImmutableArray(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadVersionedObject<T>(in StructVersion version = default) where T : IReadable, new()
    {
        var obj = new T();
        ReadVersionedObject(ref obj, version);
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadVersionedObject<T>(scoped ref T dest, in StructVersion version = default)
        where T : IReadable
    {
        dest.Read(ref this, in version);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadVersionedObject<T>(scoped Span<T> dest, in StructVersion version = default)
        where T : IReadable
    {
        for (int i = 0; i < dest.Length; i++)
        {
            ReadVersionedObject(ref dest[i], version);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> ReadVersionedObjectArray<T>(long count, in StructVersion version = default)
        where T : IReadable, new()
    {
        var array = GC.AllocateUninitializedArray<T>((int)count);
        ReadVersionedObject(array, in version);
        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadNativeUInt()
        => Config.Is32Bit
            ? ReadPrimitive<uint>()
            : ReadPrimitive<ulong>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadNativeInt()
        => Config.Is32Bit
            ? ReadPrimitive<int>()
            : ReadPrimitive<long>();
}
