namespace VersionedSerialization;

public interface ISeekableReader : IReader
{
    int Offset { get; set; }
    int Length { get; }
}