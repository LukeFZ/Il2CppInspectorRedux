using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace VersionedSerialization.Impl;

// ReSharper disable ReplaceSliceWithRangeIndexer | The range indexer gets compiled into .Slice(x, y) and not .Slice(x) which worsens performance
public ref struct SpanReader(ReadOnlySpan<byte> data, int offset = 0)
    : ISeekableReader
{
    public int Offset { get; set; } = offset;

    public readonly byte Peek => _data[Offset];
    public readonly int Length => _data.Length;

    private readonly ReadOnlySpan<byte> _data = data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(scoped Span<T> dest) where T : unmanaged
    {
        var offset = Offset;
        var data = MemoryMarshal.Cast<T, byte>(dest);
        _data.Slice(offset, data.Length).CopyTo(data);
        Offset = offset + data.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged
        => Read(dest);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(long length)
    {
        var intLength = checked((int)length);

        var offset = Offset;
        var val = _data.Slice(offset, intLength);
        Offset = offset + intLength;
        return val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(int length = -1, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        if (encoding is not (UTF8Encoding or ASCIIEncoding))
            ThrowUnsupportedEncodingException();

        var offset = Offset;
        if (length == -1)
        {
            length = _data.Slice(offset).IndexOf(byte.MinValue);
            if (length == -1)
                throw new InvalidDataException("Failed to find string in span.");
        }

        var val = _data.Slice(offset, length);
        var str = encoding.GetString(val);

        Offset = offset + length + 1; // Skip null terminator

        return str;
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedEncodingException() =>
        throw new InvalidOperationException("Unsupported encoding: Only ASCII/UTF8 is currently supported.");
}