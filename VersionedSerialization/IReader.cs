using System.Text;

namespace VersionedSerialization;

public interface IReader
{
    string ReadString(int length = -1, Encoding? encoding = null);
    ReadOnlySpan<byte> ReadBytes(long length);

    void Read<T>(scoped Span<T> dest) where T : unmanaged;
    void ReadPrimitive<T>(scoped Span<T> dest) where T : unmanaged;
}