using System.Numerics;
using System.Runtime.CompilerServices;
using VersionedSerialization.Impl;

namespace VersionedSerialization;

file static class ReaderAccessors<TReader>
    where TReader : IReader, allows ref struct
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_impl")]
    public static extern ref TReader GetImpl(ref Reader<TReader> obj);
}

public static class SeekableReaderExtensions
{
    extension<TReader>(ref Reader<TReader> reader)
        where TReader : ISeekableReader, allows ref struct
    {
        public int Offset
        {
            get => ReaderAccessors<TReader>.GetImpl(ref reader).Offset;
            set => ReaderAccessors<TReader>.GetImpl(ref reader).Offset = value;
        }

        public int Length => ReaderAccessors<TReader>.GetImpl(ref reader).Length;

        public void Align(int alignment = 0)
        {
            if (alignment == 0)
            {
                alignment = reader.Config.Is32Bit
                    ? sizeof(int)
                    : sizeof(long);
            }

            if (BitOperations.IsPow2(alignment))
            {
                reader.Offset = (reader.Offset + (alignment - 1)) & ~(alignment - 1);
            }
            else
            {
                var offset = reader.Offset;

                var rem = offset % alignment;
                if (rem != 0)
                {
                    reader.Offset += alignment - rem;
                }
            }
        }

        public void Skip(int count)
        {
            reader.Offset += count;
        }
    }

    extension<TReader>(TReader reader) where TReader : ISeekableReader, allows ref struct
    {
        public LittleEndianSeekableReader<TReader> AsLittleEndian() => new(reader);
        public BigEndianSeekableReader<TReader> AsBigEndian() => new(reader);
    }
}