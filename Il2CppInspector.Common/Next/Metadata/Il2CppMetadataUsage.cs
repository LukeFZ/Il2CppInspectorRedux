using System.Diagnostics;
using VersionedSerialization;

namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;
using EncodedMethodIndex = uint;

[VersionedStruct]
public partial record struct Il2CppMetadataUsage
{
    private const uint TypeMask = 0b111u << 29;
    private const uint InflatedMask = 0b1;
    private const uint IndexMask = ~(TypeMask | InflatedMask);

    public readonly Il2CppMetadataUsageType Type => (Il2CppMetadataUsageType)((EncodedValue & TypeMask) >> 29);
    public readonly uint Index => (EncodedValue & IndexMask) >> 1;
    public readonly bool Inflated => (EncodedValue & InflatedMask) == 1;

    public EncodedMethodIndex EncodedValue;

    public static Il2CppMetadataUsage FromValue(in StructVersion version, uint encodedValue)
    {
        /* Post v19: These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
        /* Below v19: These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */

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

            var type = (encodedValue & TypeMask) >> 29;
            var value = encodedValue & (IndexMask | 1);
            Debug.Assert((value & 0x10000000) == 0);

            return new Il2CppMetadataUsage
            {
                EncodedValue = (type << 29) | (value << 1)
            };
        }

        var methodType = (encodedValue >> 31) != 0
            ? Il2CppMetadataUsageType.MethodRef 
            : Il2CppMetadataUsageType.MethodDef;

        var index = encodedValue & 0x7FFFFFFF;
        Debug.Assert((index & 0x60000000) == 0);

        return new Il2CppMetadataUsage
        {
            EncodedValue = ((uint)methodType << 29) | (index << 1)
        };
    }

    public readonly override string ToString()
    {
        return $"{Type} @ 0x{Index:X}";
    }
}