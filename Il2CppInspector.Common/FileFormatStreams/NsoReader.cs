#nullable enable

using K4os.Compression.LZ4;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Il2CppInspector.Next;
using VersionedSerialization;

namespace Il2CppInspector;

// These are slimmed-down versions of the project the loader originated from,
// which is why the API might look a bit weird
internal class ByteArrayBackingBuffer : IDisposable
{
    [MemberNotNullWhen(true, nameof(Buffer))]
    public bool Initialized { get; protected set; }

    public bool IsLittleEndian { get; protected set; }
    public bool Is32Bit { get; protected set; }
    public ulong BaseAddress { get; protected set; }
    public byte[]? Buffer { get; private set; }

    protected Span<byte> Data => Buffer.AsSpan();

    public void Initialize(long capacity, bool littleEndian, bool is32Bit, ulong baseAddress)
    {
        IsLittleEndian = littleEndian;
        Is32Bit = is32Bit;
        BaseAddress = baseAddress;
        Buffer = new byte[capacity];
        Initialized = true;
    }

    protected int TranslateVaToRva(ulong address)
    {
        Debug.Assert(address >= BaseAddress);
        var relativeAddress = address - BaseAddress;
        return checked((int)relativeAddress);
    }

    public T ReadObject<T>(ulong address, in StructVersion version) where T : unmanaged, IReadable
    {
        Debug.Assert(Initialized);

        return IsLittleEndian
            ? Reader.LittleEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadVersionedObject<T>(in version)
            : Reader.BigEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadVersionedObject<T>(in version);
    }

    public ImmutableArray<T> ReadObjectArray<T>(ulong address, long count, in StructVersion version) where T : unmanaged, IReadable
    {
        if (count == 0)
            return [];

        Debug.Assert(Initialized);

        return IsLittleEndian
            ? Reader.LittleEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadVersionedObjectArray<T>(count, in version)
            : Reader.BigEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadVersionedObjectArray<T>(count, in version);
    }

    public T ReadPrimitive<T>(ulong address) where T : unmanaged
    {
        Debug.Assert(Initialized);

        return IsLittleEndian
            ? Reader.LittleEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadPrimitive<T>()
            : Reader.BigEndian(Data, TranslateVaToRva(address), new ReaderConfig(Is32Bit))
                .ReadPrimitive<T>();
    }

    public void WriteNUInt(ulong address, ulong value)
    {
        var region = Data.Slice(TranslateVaToRva(address), Is32Bit ? sizeof(uint) : sizeof(ulong));
        if (Is32Bit)
        {
            if (IsLittleEndian)
                BinaryPrimitives.WriteUInt32LittleEndian(region, (uint)value);
            else
                BinaryPrimitives.WriteUInt32BigEndian(region, (uint)value);
        }
        else
        {
            if (IsLittleEndian)
                BinaryPrimitives.WriteUInt64LittleEndian(region, value);
            else
                BinaryPrimitives.WriteUInt64BigEndian(region, value);
        }
    }

    public void WriteBytes(ulong address, ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Data.Slice(TranslateVaToRva(address), bytes.Length));
    }

    public void Clear(ulong address, long size)
    {
        Data.Slice(TranslateVaToRva(address), checked((int)size)).Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class NsoReader : FileFormatStream<NsoReader>
{
    public override string DefaultFilename => "main";
    public override int Bits => _is32Bit ? 32 : 64;
    public override string Arch => "ARM64";
    public override string Format => "NSO";

    // NOTE: This does not work for some reason?
    // public override ulong ImageBase => 0x7100000000;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mappedExecutable.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool Init()
    {
        var magic = ReadPrimitive<uint>();
        if (magic != NsoHeader.ExpectedMagic)
            return false;

        Position = 0;
        LoadInternal();

        Position = 0;
        Write(_mappedExecutable.Buffer);

        return true;
    }

    private readonly ByteArrayBackingBuffer _mappedExecutable = new();
    private FrozenDictionary<DynamicTag, ulong> _dynamicEntries = FrozenDictionary<DynamicTag, ulong>.Empty;
    private ImmutableArray<(ulong Start, ulong End)> _relocationEntryRegions = [];
    private bool _is32Bit;

    private void LoadInternal()
    {
        var header = ReadVersionedObject<NsoHeader>();

        var totalLength = header.TextSegment.Size + header.RoSegment.Size + header.DataSegment.Size + header.BssSize;

        _mappedExecutable.Initialize(totalLength, true, false, 0);

        LoadSegment(in header.TextSegment, header.TextFileSize, header.Flags.HasFlag(SegmentFlags.TextCompress));
        LoadSegment(in header.RoSegment, header.RoFileSize, header.Flags.HasFlag(SegmentFlags.RoCompress));
        LoadSegment(in header.DataSegment, header.DataFileSize, header.Flags.HasFlag(SegmentFlags.DataCompress));

        LoadMappedExecutable(header.TextSegment.MemoryOffset);
    }

    private void LoadMappedExecutable(uint textSegmentRva)
    {
        var modInfoHeader = _mappedExecutable.ReadObject<ModInfoHeader>(textSegmentRva, default);
        var modHeader = _mappedExecutable.ReadObject<ModHeader>(modInfoHeader.ModOffset, default);

        Debug.Assert(modHeader.ValidMagic);

        // check if we are loading a 32-bit binary
        // by checking if we have a full .dynamic entry in the first (or third) dynamic slot
        var firstEntry = _mappedExecutable.ReadPrimitive<ulong>(modInfoHeader.ModOffset + modHeader.DynamicOffset);
        var thirdEntry = _mappedExecutable.ReadPrimitive<ulong>(modInfoHeader.ModOffset + modHeader.DynamicOffset + (2 * 0x8));
        _is32Bit = firstEntry > uint.MaxValue || thirdEntry > uint.MaxValue;

        var currentDynamicOffset = modInfoHeader.ModOffset + modHeader.DynamicOffset;
        var dynamicEntries = new Dictionary<DynamicTag, ulong>();
        var dynamicEntrySize = (uint)DynamicEntry.StructSize(config: new ReaderConfig(Is32Bit));

        while (true)
        {
            var entry = _mappedExecutable.ReadObject<DynamicEntry>(currentDynamicOffset, default);
            if (entry.Tag == DynamicTag.DT_NULL)
                break;

            dynamicEntries[entry.Tag] = entry.Value;
            currentDynamicOffset += dynamicEntrySize;
        }

        _dynamicEntries = dynamicEntries.ToFrozenDictionary();

        ApplyRelocations();
    }

    private void LoadSegment(ref readonly SegmentHeader header, uint fileSize, bool isCompressed)
    {
        var reader = new Reader<BinaryObjectStreamReader>(this)
        {
            Offset = checked((int)header.FileOffset)
        };
        var data = reader.ReadBytes(checked((int)fileSize));

        if (!isCompressed)
        {
            Debug.Assert(header.Size == fileSize);
            _mappedExecutable.WriteBytes(header.MemoryOffset, data);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(checked((int)header.Size));
            var decompressed = rented.AsSpan(0, checked((int)header.Size));

            var result = LZ4Codec.Decode(data, decompressed);
            Debug.Assert(result == header.Size);
            _ = result;

            _mappedExecutable.WriteBytes(header.MemoryOffset, decompressed);

            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    // Copied and trimmed down from ElfBinary
    private void ApplyRelocations()
    {
        if (!_dynamicEntries.TryGetValue(DynamicTag.DT_SYMTAB, out var symtabAddress)
            || !_dynamicEntries.TryGetValue(DynamicTag.DT_SYMENT, out var symtabEntrySize))
            return;

        List<(ulong Start, ulong End)> relocationRegions = [];

        if (_dynamicEntries.TryGetValue(DynamicTag.DT_REL, out var relAddress))
        {
            var relocationSize = _dynamicEntries[DynamicTag.DT_RELSZ];
            ApplyRelocationsImpl(ParseRelSection(relAddress, relocationSize));
            relocationRegions.Add((relAddress, relAddress + relocationSize));
        }
        else if (_dynamicEntries.TryGetValue(DynamicTag.DT_RELA, out var relaAddress))
        {
            var relocationSize = _dynamicEntries[DynamicTag.DT_RELASZ];
            ApplyRelocationsImpl(ParseRelaSection(relaAddress, relocationSize));
            relocationRegions.Add((relaAddress, relaAddress + relocationSize));
        }

        if (_dynamicEntries.TryGetValue(DynamicTag.DT_JMPREL, out var jmprelAddress))
        {
            var size = _dynamicEntries[DynamicTag.DT_PLTRELSZ];
            var type = (DynamicTag)_dynamicEntries[DynamicTag.DT_PLTREL];
            ApplyRelocationsImpl(type == DynamicTag.DT_REL
                ? ParseRelSection(jmprelAddress, size)
                : ParseRelaSection(jmprelAddress, size));

            relocationRegions.Add((jmprelAddress, jmprelAddress + size));
        }

        _relocationEntryRegions = [.. relocationRegions];

        // Clear out relocation sections in memory so searching is faster
        foreach (var (start, end) in _relocationEntryRegions)
        {
            _mappedExecutable.Clear(start, checked((long)(end - start)));
        }

        return;

        void ApplyRelocationsImpl(ICollection<(ulong Offset, ulong Info, long? Addend)> relocations)
        {
            if (relocations.Count == 0)
                return;

            var symbolCache = new Dictionary<uint, SymbolEntry>();

            foreach (var relocation in relocations)
            {
                var offset = relocation.Offset;
                var type = (RelocationType)(Is32Bit ? relocation.Info & byte.MaxValue : relocation.Info & uint.MaxValue);
                var symbolIndex = (uint)(relocation.Info >> (Is32Bit ? 8 : 32));
                var addend = checked((ulong)(relocation.Addend ?? _mappedExecutable.ReadPrimitive<long>(offset)));

                if (!symbolCache.TryGetValue(symbolIndex, out var symbolEntry))
                {
                    var symtabEntryAddress = symtabAddress + symbolIndex * symtabEntrySize;
                    symbolEntry = _mappedExecutable.ReadObject<SymbolEntry>(symtabEntryAddress, default);
                    symbolCache[symbolIndex] = symbolEntry;
                }

                var (value, handled) = type switch
                {
                    RelocationType.R_ARM_ABS32 or RelocationType.R_AARCH64_ABS64 => (symbolEntry.Value + addend, true),
                    RelocationType.R_ARM_REL32 or RelocationType.R_AARCH64_PREL64 => (symbolEntry.Value + relocation.Offset - addend, true),
                    RelocationType.R_ARM_COPY => (symbolEntry.Value, true),
                    RelocationType.R_AARCH64_GLOB_DAT => (symbolEntry.Value + addend, true),
                    RelocationType.R_AARCH64_JUMP_SLOT => (symbolEntry.Value + addend, true),
                    RelocationType.R_AARCH64_RELATIVE => (symbolEntry.Value + addend, true),
                    _ => (0uL, false)
                };

                if (handled)
                {
                    _mappedExecutable.WriteNUInt(offset, value);
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        List<(ulong, ulong, long?)> ParseRelSection(ulong address, ulong size)
        {
            var entrySize = _dynamicEntries[DynamicTag.DT_RELENT];
            var entryCount = size / entrySize;

            return _mappedExecutable.ReadObjectArray<RelEntry>(address, checked((int)entryCount), default)
                .Select<RelEntry, (ulong, ulong, long?)>(x => (x.Offset, x.Info, null))
                .ToList();
        }

        List<(ulong, ulong, long?)> ParseRelaSection(ulong address, ulong size)
        {
            var entrySize = _dynamicEntries[DynamicTag.DT_RELAENT];
            var entryCount = size / entrySize;

            return _mappedExecutable.ReadObjectArray<RelaEntry>(address, checked((int)entryCount), default)
                .Select<RelaEntry, (ulong, ulong, long?)>(x => (x.Offset, x.Info, x.Addend))
                .ToList();
        }
    }
}