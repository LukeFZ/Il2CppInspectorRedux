using System.Runtime.CompilerServices;
using VersionedSerialization.Impl;

namespace VersionedSerialization;

public static class ReaderExtensions
{
    extension<TReader>(ref Reader<TReader> reader)
        where TReader : struct, IReader, allows ref struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadCompressedUInt()
        {
            var first = reader.ReadPrimitive<byte>();

            if ((first & 0b10000000) == 0b00000000)
                return first;

            if ((first & 0b11000000) == 0b10000000)
                return (uint)(((first & ~0b10000000) << 8) | reader.ReadPrimitive<byte>());

            if ((first & 0b11100000) == 0b11000000)
                return (uint)(((first & ~0b11000000) << 24) | (reader.ReadPrimitive<byte>() << 16) | (reader.ReadPrimitive<byte>() << 8) | reader.ReadPrimitive<byte>());

            return first switch
            {
                0b11110000 => reader.ReadPrimitive<uint>(),
                0b11111110 => uint.MaxValue - 1,
                0b11111111 => uint.MaxValue,
                _ => throw new InvalidDataException("Invalid compressed uint")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadCompressedInt()
        {
            var value = reader.ReadCompressedUInt();
            if (value == uint.MaxValue)
                return int.MinValue;

            var isNegative = (value & 0b1) == 1;
            value >>= 1;

            return (int)(isNegative ? -(value + 1) : value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadSLEB128()
        {
            var value = 0uL;
            var shift = 0;
            byte current;

            do
            {
                current = reader.ReadPrimitive<byte>();
                value |= (current & 0x7FuL) << shift;
                shift += 7;
            } while ((current & 0x80) != 0);

            if (64 >= shift && (current & 0x40) != 0)
                value |= ulong.MaxValue << shift;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
            => reader.ReadPrimitive<byte>() != 0;
    }

    extension<TReader>(TReader reader) where TReader : INonSeekableReader, allows ref struct
    {
        public LittleEndianReader<TReader> AsLittleEndian() => new(reader);
        public BigEndianReader<TReader> AsBigEndian() => new(reader);
    }
}