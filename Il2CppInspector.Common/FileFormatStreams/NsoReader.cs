using Il2CppInspector.Next;
using K4os.Compression.LZ4;

namespace Il2CppInspector
{
    // Nintendo Switch NSO executable reader.
    // NSO uses ELF-style dynamic metadata but stores segments in a custom container.
    public class NsoReader : FileFormatStream<NsoReader>
    {
        private const uint NsoMagic = 0x304F534E; // NSO0
        private const ulong DtGnuHash = 0x6ffffef5;

        public override string DefaultFilename => "main";

        public override string Format => "NSO";

        public override string Arch => "ARM64";

        public override int Bits => 64;

        private readonly List<NsoSegment> _segments = [];
        private readonly List<elf_dynamic<ulong>> _dynamicTable = [];
        private readonly Dictionary<string, Symbol> _symbolTable = new();
        private readonly List<Export> _exports = [];

        private NsoHeader _header = null!;
        private elf_64_sym[] _symbols = [];

        public override ulong ImageBase => GlobalOffset;

        protected override bool Init()
        {
            if (!ReadHeader())
                return false;

            if (_header.IsCompressed)
            {
                DecompressImage();
                Position = 0;

                if (!ReadHeader())
                    throw new InvalidOperationException("Failed to parse NSO after decompression.");

                IsModified = true;
            }

            GlobalOffset = _header.TextSegment.MemoryOffset - _header.TextSegment.FileOffset;

            ReadMod0AndDynamicTable();
            ReadSymbols();
            ApplyRelocations();
            BuildSymbolTable();

            return true;
        }

        private bool ReadHeader()
        {
            Position = 0;

            if (Length < 0x100)
                return false;

            var magic = ReadUInt32();
            if (magic != NsoMagic)
                return false;

            _segments.Clear();
            _dynamicTable.Clear();
            _symbolTable.Clear();
            _exports.Clear();
            _symbols = [];

            _header = new NsoHeader
            {
                Magic = magic,
                Version = ReadUInt32(),
                Reserved = ReadUInt32(),
                Flags = ReadUInt32(),
                TextSegment = ReadSegment("text", isExec: true, isData: false),
                ModuleOffset = ReadUInt32(),
                RoDataSegment = ReadSegment("rodata", isExec: false, isData: true),
                ModuleFileSize = ReadUInt32(),
                DataSegment = ReadSegment("data", isExec: false, isData: true),
                BssSize = ReadUInt32(),
                DigestBuildId = ReadBytes(0x20).ToArray(),
                TextCompressedSize = ReadUInt32(),
                RoDataCompressedSize = ReadUInt32(),
                DataCompressedSize = ReadUInt32(),
                Padding = ReadBytes(0x1C).ToArray(),
                ApiInfo = ReadExtent(),
                DynStr = ReadExtent(),
                DynSym = ReadExtent(),
                TextHash = ReadBytes(0x20).ToArray(),
                RoDataHash = ReadBytes(0x20).ToArray(),
                DataHash = ReadBytes(0x20).ToArray()
            };

            _segments.Add(_header.TextSegment);
            _segments.Add(_header.RoDataSegment);
            _segments.Add(_header.DataSegment);

            return true;
        }

        private NsoSegment ReadSegment(string name, bool isExec, bool isData)
        {
            return new NsoSegment
            {
                Name = name,
                FileOffset = ReadUInt32(),
                MemoryOffset = ReadUInt32(),
                DecompressedSize = ReadUInt32(),
                IsExec = isExec,
                IsData = isData
            };
        }

        private static NsoRelativeExtent ReadExtent(BinaryObjectStreamReader reader)
        {
            return new NsoRelativeExtent
            {
                RegionRoDataOffset = reader.ReadUInt32(),
                RegionSize = reader.ReadUInt32()
            };
        }

        private NsoRelativeExtent ReadExtent() => ReadExtent(this);

        private void DecompressImage()
        {
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output);

            writer.Write(_header.Magic);
            writer.Write(_header.Version);
            writer.Write(_header.Reserved);
            writer.Write(0u);

            writer.Write(_header.TextSegment.FileOffset);
            writer.Write(_header.TextSegment.MemoryOffset);
            writer.Write(_header.TextSegment.DecompressedSize);
            writer.Write(_header.ModuleOffset);

            var roDataOffset = _header.TextSegment.FileOffset + _header.TextSegment.DecompressedSize;
            writer.Write(roDataOffset);
            writer.Write(_header.RoDataSegment.MemoryOffset);
            writer.Write(_header.RoDataSegment.DecompressedSize);
            writer.Write(_header.ModuleFileSize);

            var dataOffset = roDataOffset + _header.RoDataSegment.DecompressedSize;
            writer.Write(dataOffset);
            writer.Write(_header.DataSegment.MemoryOffset);
            writer.Write(_header.DataSegment.DecompressedSize);
            writer.Write(_header.BssSize);

            writer.Write(_header.DigestBuildId);
            writer.Write(_header.TextCompressedSize);
            writer.Write(_header.RoDataCompressedSize);
            writer.Write(_header.DataCompressedSize);
            writer.Write(_header.Padding);

            writer.Write(_header.ApiInfo.RegionRoDataOffset);
            writer.Write(_header.ApiInfo.RegionSize);
            writer.Write(_header.DynStr.RegionRoDataOffset);
            writer.Write(_header.DynStr.RegionSize);
            writer.Write(_header.DynSym.RegionRoDataOffset);
            writer.Write(_header.DynSym.RegionSize);

            writer.Write(_header.TextHash);
            writer.Write(_header.RoDataHash);
            writer.Write(_header.DataHash);

            writer.BaseStream.Position = _header.TextSegment.FileOffset;

            WriteDecompressedSegment(writer, _header.TextSegment, _header.TextCompressedSize, _header.IsTextCompressed);
            WriteDecompressedSegment(writer, _header.RoDataSegment, _header.RoDataCompressedSize, _header.IsRoDataCompressed);
            WriteDecompressedSegment(writer, _header.DataSegment, _header.DataCompressedSize, _header.IsDataCompressed);

            writer.Flush();

            var bytes = output.ToArray();
            SetLength(0);
            Position = 0;
            Write(bytes, 0, bytes.Length);
            Position = 0;
        }

        private void WriteDecompressedSegment(BinaryWriter writer, NsoSegment segment, uint compressedSize, bool isCompressed)
        {
            Position = segment.FileOffset;
            var bytes = ReadBytes((int)compressedSize).ToArray();

            if (!isCompressed)
            {
                writer.Write(bytes);
                return;
            }

            var decompressed = new byte[segment.DecompressedSize];
            var decoded = LZ4Codec.Decode(bytes, 0, bytes.Length, decompressed, 0, decompressed.Length);
            if (decoded != decompressed.Length)
                throw new InvalidOperationException($"Failed to fully decompress NSO segment {segment.Name}.");

            writer.Write(decompressed);
        }

        private void ReadMod0AndDynamicTable()
        {
            Position = _header.TextSegment.FileOffset + 4;
            var modValue = ReadUInt32();
            ulong modBase = 0;

            foreach (var candidate in new[] {
                         _header.TextSegment.MemoryOffset + modValue,
                         (ulong) modValue
                     }.Distinct())
            {
                if (!TryMapVATR(candidate, out var modPosition))
                    continue;

                Position = modPosition;
                if (ReadUInt32() == 0x30444F4D) // MOD0
                {
                    modBase = candidate;
                    break;
                }
            }

            if (modBase == 0)
            {
                foreach (var segment in _segments.Where(x => !x.IsBss))
                {
                    Position = segment.FileOffset;
                    var bytes = ReadBytes((int)segment.DecompressedSize).ToArray();

                    for (var i = 0; i <= bytes.Length - 4; i++)
                    {
                        if (bytes[i] != (byte)'M' || bytes[i + 1] != (byte)'O' || bytes[i + 2] != (byte)'D' || bytes[i + 3] != (byte)'0')
                            continue;

                        modBase = segment.MemoryOffset + (uint)i;
                        break;
                    }

                    if (modBase != 0)
                        break;
                }
            }

            if (modBase == 0)
                throw new InvalidOperationException("NSO MOD0 header not found.");

            Position = MapVATR(modBase) + 4;
            var dynamicOffset = ReadUInt32() + modBase;
            var bssStart = ReadUInt32() + modBase;
            var bssEnd = ReadUInt32() + modBase;

            _header.BssSegment = new NsoSegment
            {
                Name = "bss",
                FileOffset = (uint)bssStart,
                MemoryOffset = (uint)bssStart,
                DecompressedSize = (uint)(bssEnd - bssStart),
                IsBss = true,
                IsData = true
            };

            _segments.Add(_header.BssSegment);

            var dynamicEntriesLimit = (_header.DataSegment.MemoryOffset + _header.DataSegment.DecompressedSize - dynamicOffset) / 0x10u;

            Position = MapVATR(dynamicOffset);
            for (var i = 0u; i < dynamicEntriesLimit; i++)
            {
                var entry = ReadObject<elf_dynamic<ulong>>();
                if (entry.d_tag == 0)
                    break;

                _dynamicTable.Add(entry);
            }
        }

        private elf_dynamic<ulong> GetDynamicEntry(ulong tag) => _dynamicTable.FirstOrDefault(x => x.d_tag == tag);

        private void ReadSymbols()
        {
            var symbolCount = GetSymbolCount();
            if (symbolCount == 0)
                return;

            var symtab = GetDynamicEntry((ulong)Elf.DT_SYMTAB);
            if (symtab == null)
                return;

            _symbols = ReadArray<elf_64_sym>(MapVATR(symtab.d_un), checked((int)symbolCount));
        }

        private uint GetSymbolCount()
        {
            var hash = GetDynamicEntry((ulong)Elf.DT_HASH);
            if (hash != null)
            {
                Position = MapVATR(hash.d_un);
                _ = ReadUInt32();
                return ReadUInt32();
            }

            hash = GetDynamicEntry(DtGnuHash);
            if (hash == null)
                return 0;

            var hashOffset = MapVATR(hash.d_un);
            Position = hashOffset;

            var bucketCount = ReadUInt32();
            var symbolOffset = ReadUInt32();
            var bloomSize = ReadUInt32();
            _ = ReadUInt32();

            var bucketsOffset = hashOffset + 0x10 + (uint)(8 * bloomSize);
            var buckets = ReadArray<uint>(bucketsOffset, checked((int)bucketCount));
            var lastSymbol = buckets.Max();

            if (lastSymbol < symbolOffset)
                return symbolOffset;

            var chainsOffset = bucketsOffset + 4 * bucketCount;
            Position = chainsOffset + (lastSymbol - symbolOffset) * 4u;

            while (true)
            {
                var chainEntry = ReadUInt32();
                lastSymbol++;

                if ((chainEntry & 1) != 0)
                    return lastSymbol;
            }
        }

        private void ApplyRelocations()
        {
            var rela = GetDynamicEntry((ulong)Elf.DT_RELA);
            var relaSize = GetDynamicEntry((ulong)Elf.DT_RELASZ);
            if (rela == null || relaSize == null)
                return;

            var relocations = ReadArray<elf_rela<ulong>>(MapVATR(rela.d_un), checked((int)(relaSize.d_un / 0x18)));

            foreach (var relocation in relocations)
            {
                var type = (Elf)(relocation.r_info & 0xffff_ffff);
                var symbolIndex = (uint)(relocation.r_info >> 32);
                ulong value;

                switch (type)
                {
                    case Elf.R_AARCH64_ABS64:
                    case Elf.R_AARCH64_GLOB_DAT:
                    case Elf.R_AARCH64_JUMP_SLOT:
                        if (symbolIndex >= _symbols.Length)
                            continue;

                        value = _symbols[symbolIndex].st_value + relocation.r_addend;
                        break;

                    case Elf.R_AARCH64_RELATIVE:
                        value = relocation.r_addend;
                        break;

                    default:
                        continue;
                }

                if (!TryMapVATR(relocation.r_offset, out var fileOffset))
                    continue;

                Write(fileOffset, value);
            }
        }

        private void BuildSymbolTable()
        {
            if (_symbols.Length == 0)
                return;

            var strtab = GetDynamicEntry((ulong)Elf.DT_STRTAB);
            var strtabSize = GetDynamicEntry((ulong)Elf.DT_STRSZ);
            if (strtab == null || strtabSize == null)
                return;

            var strtabStart = strtab.d_un;
            var strtabLength = strtabSize.d_un;

            foreach (var symbol in _symbols)
            {
                if (symbol.st_name >= strtabLength)
                    continue;

                string name;
                try
                {
                    name = ReadMappedNullTerminatedString(strtabStart + symbol.st_name);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(name))
                    continue;

                var type = symbol.type == Elf.STT_FUNC
                    ? SymbolType.Function
                    : symbol.type == Elf.STT_OBJECT || symbol.type == Elf.STT_COMMON
                        ? SymbolType.Name
                        : SymbolType.Unknown;

                if (symbol.st_shndx == (ushort)Elf.SHN_UNDEF)
                    type = SymbolType.Import;

                var symbolItem = new Symbol
                {
                    Name = name,
                    Type = type,
                    VirtualAddress = symbol.st_value
                };

                _symbolTable.TryAdd(name, symbolItem);
                if (symbol.st_shndx != (ushort)Elf.SHN_UNDEF)
                    _exports.Add(new Export { Name = name, VirtualAddress = symbol.st_value });
            }
        }

        public override Dictionary<string, Symbol> GetSymbolTable() => _symbolTable;

        public override IEnumerable<Export> GetExports() => _exports;

        public override uint[] GetFunctionTable()
        {
            var initFunctions = new List<uint>();

            var initArray = GetDynamicEntry((ulong)Elf.DT_INIT_ARRAY);
            var initArraySize = GetDynamicEntry((ulong)Elf.DT_INIT_ARRAYSZ);

            if (initArray != null && initArraySize != null)
            {
                var count = checked((int)(initArraySize.d_un / 8));
                foreach (var address in ReadMappedUWordArray(initArray.d_un, count))
                {
                    if (address != 0 && TryMapVATR(address, out var offset))
                        initFunctions.Add(offset);
                }
            }

            var init = GetDynamicEntry((ulong)Elf.DT_INIT);
            if (init != null && init.d_un != 0 && TryMapVATR(init.d_un, out var initOffset))
                initFunctions.Add(initOffset);

            return initFunctions.Distinct().ToArray();
        }

        public override IEnumerable<Section> GetSections()
        {
            return _segments.Select(segment => new Section
            {
                VirtualStart = segment.MemoryOffset,
                VirtualEnd = segment.MemoryOffset + segment.DecompressedSize - 1,
                ImageStart = segment.FileOffset,
                ImageEnd = segment.DecompressedSize == 0 ? segment.FileOffset : segment.FileOffset + segment.DecompressedSize - 1,
                IsExec = segment.IsExec,
                IsData = segment.IsData,
                IsBSS = segment.IsBss,
                Name = segment.Name
            });
        }

        public override uint MapVATR(ulong uiAddr)
        {
            if (!TryMapVATR(uiAddr, out var fileOffset))
                throw new InvalidOperationException("Failed to map virtual address");

            return fileOffset;
        }

        public override bool TryMapVATR(ulong uiAddr, out uint fileOffset)
        {
            if (uiAddr == 0)
            {
                fileOffset = 0;
                return true;
            }

            var segment = _segments.FirstOrDefault(x =>
                !x.IsBss &&
                uiAddr >= x.MemoryOffset &&
                uiAddr < x.MemoryOffset + x.DecompressedSize);

            if (segment == null)
            {
                fileOffset = 0;
                return false;
            }

            var offset = segment.FileOffset + (uiAddr - segment.MemoryOffset);
            if (offset >= (ulong)Length)
            {
                fileOffset = 0;
                return false;
            }

            fileOffset = (uint)offset;
            return true;
        }

        public override ulong MapFileOffsetToVA(uint offset)
        {
            var segment = _segments.FirstOrDefault(x =>
                !x.IsBss &&
                offset >= x.FileOffset &&
                offset < x.FileOffset + x.DecompressedSize);

            if (segment == null)
                throw new InvalidOperationException("Failed to map file offset");

            return segment.MemoryOffset + offset - segment.FileOffset;
        }

        private sealed class NsoHeader
        {
            public uint Magic;
            public uint Version;
            public uint Reserved;
            public uint Flags;
            public NsoSegment TextSegment = null!;
            public uint ModuleOffset;
            public NsoSegment RoDataSegment = null!;
            public uint ModuleFileSize;
            public NsoSegment DataSegment = null!;
            public uint BssSize;
            public byte[] DigestBuildId = [];
            public uint TextCompressedSize;
            public uint RoDataCompressedSize;
            public uint DataCompressedSize;
            public byte[] Padding = [];
            public NsoRelativeExtent ApiInfo = null!;
            public NsoRelativeExtent DynStr = null!;
            public NsoRelativeExtent DynSym = null!;
            public byte[] TextHash = [];
            public byte[] RoDataHash = [];
            public byte[] DataHash = [];
            public NsoSegment BssSegment = null!;

            public bool IsTextCompressed => (Flags & 1) != 0;
            public bool IsRoDataCompressed => (Flags & 2) != 0;
            public bool IsDataCompressed => (Flags & 4) != 0;
            public bool IsCompressed => IsTextCompressed || IsRoDataCompressed || IsDataCompressed;
        }

        private sealed class NsoSegment
        {
            public string Name = string.Empty;
            public uint FileOffset;
            public uint MemoryOffset;
            public uint DecompressedSize;
            public bool IsExec;
            public bool IsData;
            public bool IsBss;
        }

        private sealed class NsoRelativeExtent
        {
            public uint RegionRoDataOffset;
            public uint RegionSize;
        }
    }
}
