using System.Diagnostics;
using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;
using EncodedMethodIndex = uint;

[VersionedStruct]
public partial record struct Il2CppMetadataUsage
{
    private const int TypeShift = 29;
    private const int IndexShift = 1;

    private const uint TypeMask = 0b111u << TypeShift;
    private const uint InflatedMask = 0b1;
    private const uint IndexMask = ~(TypeMask | InflatedMask);

    public readonly Il2CppMetadataUsageType Type => (Il2CppMetadataUsageType)((EncodedValue & TypeMask) >> TypeShift);
    public readonly uint Index => (EncodedValue & IndexMask) >> IndexShift;
    public readonly bool Inflated => (EncodedValue & InflatedMask) == 1;

    public EncodedMethodIndex EncodedValue;

    private static Il2CppMetadataUsage Create(Il2CppMetadataUsageType type, uint index, bool inflated = false) =>
        new()
        {
            EncodedValue = ((uint)type << TypeShift) | (index << IndexShift) | (inflated ? 1u : 0),
        };

    public static Il2CppMetadataUsage FromValue(in StructVersion version, uint encodedValue)
    {
        /* Post v19: These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
        /* Below v19: These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */

        // v106.1 removed kMetadataUsageIl2CppType from the enum, but didn't change the other values :( so we have to manually fix it for our enum
        if (version >= MetadataVersions.V1061)
        {
            var type = (Il2CppMetadataUsageType)((encodedValue & TypeMask) >> TypeShift);
            if (type >= Il2CppMetadataUsageType.Il2CppType)
                type++;

            encodedValue = (encodedValue & ~TypeMask) | ((uint)type << TypeShift);
        }

        if (version >= MetadataVersions.V270)
        {
            return new Il2CppMetadataUsage
            {
                EncodedValue = encodedValue
            };
        }

        if (version >= MetadataVersions.V190)
        {
            // Below v27 we need to fake the 'inflated' flag, so shift the value by one

            var type = (Il2CppMetadataUsageType)((encodedValue & TypeMask) >> TypeShift);
            var value = encodedValue & (IndexMask | 1);
            Debug.Assert((value & 0x10000000) == 0);

            return Create(type, value);
        }

        var methodType = (encodedValue >> 31) != 0
            ? Il2CppMetadataUsageType.MethodRef 
            : Il2CppMetadataUsageType.MethodDef;

        var index = encodedValue & 0x7FFFFFFF;
        Debug.Assert((index & 0x60000000) == 0);

        return Create(methodType, index);
    }

    public readonly override string ToString()
    {
        return $"{Type} @ 0x{Index:X}";
    }
}