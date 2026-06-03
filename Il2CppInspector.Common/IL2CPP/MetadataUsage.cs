/*
    Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
    Copyright (c) 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using Il2CppInspector.Next;
using Il2CppInspector.Next.Metadata;

namespace Il2CppInspector
{
    public record struct MetadataUsage(Il2CppMetadataUsageType Type, int SourceIndex, ulong VirtualAddress = 0)
    {
        public Il2CppMetadataUsageType Type { get; } = Type;
        public int SourceIndex { get; } = SourceIndex;
        public ulong VirtualAddress { get; } = VirtualAddress;

        public readonly bool IsValid => Type != 0;

        public static MetadataUsage FromEncodedIndex(Il2CppInspector package, uint encodedIndex, ulong virtualAddress = 0)
        {
            var usage = Il2CppMetadataUsage.FromValue(package.Version, encodedIndex);
            if (package.Version >= MetadataVersions.V190)
            {
                return new MetadataUsage(usage.Type, (int)usage.Index, virtualAddress);
            }

            /* These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */
            var index = package.Binary.VTableMethodReferences[(int)usage.Index];
            return new MetadataUsage(usage.Type, (int)index, virtualAddress);
        }
    }
}