using System.Runtime.CompilerServices;
using VersionedSerialization;
using VersionedSerialization.Attributes;

namespace Il2CppInspector;

[VersionedStruct]
public partial struct NsoHeader
{
    public const uint ExpectedMagic = 0x304F534E; // NSO0 (LE)

    public uint Magic;
    public uint Version;
    public uint Reserved;
    public SegmentFlags Flags;

    public SegmentHeader TextSegment;
    public uint ModuleNameOffset;

    public SegmentHeader RoSegment;
    public uint ModuleNameSize;

    public SegmentHeader DataSegment;
    public uint BssSize;

    public ModuleIdBuffer ModuleId;

    public uint TextFileSize;
    public uint RoFileSize;
    public uint DataFileSize;

    public ReservedBuffer Reserved2;

    public SegmentHeaderRelative ApiInfoSection;
    public SegmentHeaderRelative DynStrSection;
    public SegmentHeaderRelative DynSymInfo;

    public HashBuffer TextHash;
    public HashBuffer RoHash;
    public HashBuffer DataHash;

    [InlineArray(32)]
    public struct ModuleIdBuffer
    {
        private byte _value0;
    }

    [InlineArray(0x1c)]
    public struct ReservedBuffer
    {
        private byte _value0;
    }

    [InlineArray(32)]
    public struct HashBuffer
    {
        private byte _value0;
    }
}

[VersionedStruct]
public partial struct SegmentHeaderRelative
{
    public uint Offset;
    public uint Size;
}

[VersionedStruct]
public partial struct SegmentHeader
{
    public uint FileOffset;
    public uint MemoryOffset;
    public uint Size;
}

[Flags]
public enum SegmentFlags : uint
{
    TextCompress = 1 << 0,
    RoCompress = 1 << 1,
    DataCompress = 1 << 2,
    TextHash = 1 << 3,
    RoHash = 1 << 4,
    DataHash = 1 << 5
}

[VersionedStruct]
public partial struct ModInfoHeader
{
    public uint Reserved;
    public uint ModOffset;
}

[VersionedStruct]
public partial struct ModHeader
{
    public const uint ExpectedMagic = 0x30444f4d; // MOD0

    public readonly bool ValidMagic => Magic == ExpectedMagic;

    public uint Magic;
    public uint DynamicOffset;
    public uint BssStartOffset;
    public uint BssEndOffset;
    public uint EhFrameHdrStartOffset;
    public uint EhFrameHdrEndOffset;
    public uint ModuleOffset;
}

// These are just regular ELF stucts, but as long as the ELF parser still
// uses the old format we'll duplicate them here

[VersionedStruct]
public partial struct DynamicEntry
{
    [NativeInteger]
    public DynamicTag Tag;

    [NativeInteger]
    public ulong Value;
}

public enum DynamicTag : long
{
    DT_NULL = 0,
    DT_NEEDED = 1,
    DT_PLTRELSZ = 2,
    DT_PLTGOT = 0x3,
    DT_HASH = 0x4,
    DT_STRTAB = 0x5,
    DT_SYMTAB = 0x6,
    DT_RELA = 0x7,
    DT_RELASZ = 0x8,
    DT_RELAENT = 0x9,
    DT_STRSZ = 0xa,
    DT_SYMENT = 0xb,
    DT_INIT = 0xC,
    DT_FINI = 0xD,
    DT_SONAME = 0xe,
    DT_RPATH = 0xf,
    DT_SYMBOLIC = 0x10,
    DT_REL = 0x11,
    DT_RELSZ = 0x12,
    DT_RELENT = 0x13,
    DT_PLTREL = 0x14,
    DT_DEBUG = 0x15,
    DT_TEXTREL = 0x16,
    DT_JMPREL = 0x17,
    DT_BIND_NOW = 0x18,
    DT_INIT_ARRAY = 0x19,
    DT_FINI_ARRAY = 0x1A,
    DT_INIT_ARRAYSZ = 0x1B,
    DT_FINI_ARRAYSZ = 0x1C,
    DT_RUNPATH = 0x1D,
    DT_FLAGS = 0x1E,
    DT_PREINIT_ARRAY = 0x20,
    DT_PREINIT_ARRAYSZ = 0x21,
    DT_MAXPOSTAGS = 0x22,
    DT_RELRSZ = 0x23,
    DT_RELR = 0x24,
    DT_RELRENT = 0x25,
    DT_LOOS = 0x6000000D,
    DT_ANDROID_REL = DT_LOOS + 2,
    DT_ANDROID_RELSZ = DT_LOOS + 3,
    DT_ANDROID_RELA = DT_LOOS + 4,
    DT_ANDROID_RELASZ = DT_LOOS + 5,
    DT_ANDROID_RELR = 0x6fffe000,
    DT_ANDROID_RELRSZ = 0x6fffe001,
    DT_ANDROID_RELRENT = 0x6fffe003,
    DT_ANDROID_RELRCOUNT = 0x6fffe005,
}

public struct SymbolEntry : IReadable
{
    public const int Size32Bit = sizeof(uint) + sizeof(uint) + sizeof(byte) + sizeof(byte) + sizeof(ushort);
    public const int Size64Bit = sizeof(byte) + sizeof(byte) + sizeof(ushort) + sizeof(ulong) + sizeof(ulong);

    public uint Name;
    public ulong Value;
    public ulong Size;
    public byte Info;
    public byte Other;
    public ushort SectionIndex;

    public readonly ElfSymbolBind Bind => (ElfSymbolBind)(Info >> 4);
    public readonly ElfSymbolType Type => (ElfSymbolType)(Info & 0xf);

    public void Read<TReader>(ref Reader<TReader> reader, in StructVersion version = default)
        where TReader : IReader, allows ref struct
    {
        reader.ReadPrimitive(ref Name);

        if (reader.Config.Is32Bit)
        {
            Value = reader.ReadNativeUInt();
            Size = reader.ReadPrimitive<uint>();
            Info = reader.ReadPrimitive<byte>();
            Other = reader.ReadPrimitive<byte>();
            SectionIndex = reader.ReadPrimitive<ushort>();
        }
        else
        {
            Info = reader.ReadPrimitive<byte>();
            Other = reader.ReadPrimitive<byte>();
            SectionIndex = reader.ReadPrimitive<ushort>();
            Value = reader.ReadNativeUInt();
            Size = reader.ReadNativeUInt();
        }
    }

    static int IReadable.Size(in StructVersion version, in ReaderConfig config)
        => config.Is32Bit ? Size32Bit : Size64Bit;
}

public enum ElfSymbolBind
{
    STB_LOCAL,
    STB_GLOBAL,
    STB_WEAK
}

public enum ElfSymbolType
{
    STT_NOTYPE,
    STT_OBJECT,
    STT_FUNC,
    STT_SECTION,
    STT_FILE,
    STT_COMMON,
    STT_TLS
}

public enum RelocationType : uint
{
    R_ARM_ABS32 = 2,
    R_ARM_REL32 = 3,
    R_ARM_PC13 = 4,
    R_ARM_COPY = 20,

    R_AARCH64_ABS64 = 0x101,
    R_AARCH64_PREL64 = 0x104,
    R_AARCH64_GLOB_DAT = 0x401,
    R_AARCH64_JUMP_SLOT = 0x402,
    R_AARCH64_RELATIVE = 0x403,
}

[VersionedStruct]
public partial struct RelEntry
{
    [NativeInteger]
    public ulong Offset;

    [NativeInteger]
    public ulong Info;
}

[VersionedStruct]
public partial struct RelaEntry
{
    [NativeInteger]
    public ulong Offset;

    [NativeInteger]
    public ulong Info;

    [NativeInteger]
    public long Addend;
}