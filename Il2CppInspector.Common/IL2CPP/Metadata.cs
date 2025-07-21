/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Il2CppInspector.Next;
using Il2CppInspector.Next.Metadata;
using NoisyCowStudios.Bin2Object;
using VersionedSerialization;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectStreamReader
    {
        public Il2CppGlobalMetadataHeader Header { get; set; }

        public ImmutableArray<Il2CppAssemblyDefinition> Assemblies { get; set; }
        public ImmutableArray<Il2CppImageDefinition> Images { get; set; }
        public ImmutableArray<Il2CppTypeDefinition> Types { get; set; }
        public ImmutableArray<Il2CppMethodDefinition> Methods { get; set; }
        public ImmutableArray<Il2CppParameterDefinition> Params { get; set; }
        public ImmutableArray<Il2CppFieldDefinition> Fields { get; set; }
        public ImmutableArray<Il2CppFieldDefaultValue> FieldDefaultValues { get; set; }
        public ImmutableArray<Il2CppParameterDefaultValue> ParameterDefaultValues { get; set; }
        public ImmutableArray<Il2CppPropertyDefinition> Properties { get; set; }
        public ImmutableArray<Il2CppEventDefinition> Events { get; set; }
        public ImmutableArray<Il2CppGenericContainer> GenericContainers { get; set; }
        public ImmutableArray<Il2CppGenericParameter> GenericParameters { get; set; }
        public ImmutableArray<Il2CppCustomAttributeTypeRange> AttributeTypeRanges { get; set; }
        public ImmutableArray<Il2CppCustomAttributeDataRange> AttributeDataRanges { get; set; }
        public ImmutableArray<Il2CppInterfaceOffsetPair> InterfaceOffsets { get; set; }
        public ImmutableArray<Il2CppMetadataUsageList> MetadataUsageLists { get; set; }
        public ImmutableArray<Il2CppMetadataUsagePair> MetadataUsagePairs { get; set; }
        public ImmutableArray<Il2CppFieldRef> FieldRefs { get; set; }

        public ImmutableArray<int> InterfaceUsageIndices { get; set; }
        public ImmutableArray<int> NestedTypeIndices { get; set; }
        public ImmutableArray<int> AttributeTypeIndices { get; set; }
        public ImmutableArray<int> GenericConstraintIndices { get; set; }
        public ImmutableArray<uint> VTableMethodIndices { get; set; }
        public string[] StringLiterals { get; set; }

        public Dictionary<int, string> Strings { get; private set; } = new Dictionary<int, string>();

        // Set if something in the metadata has been modified / decrypted
        public bool IsModified { get; private set; } = false;

        // Status update callback
        private EventHandler<string> OnStatusUpdate { get; set; }
        private void StatusUpdate(string status) => OnStatusUpdate?.Invoke(this, status);

        // Initialize metadata object from a stream
        public static Metadata FromStream(MemoryStream stream, EventHandler<string> statusCallback = null) {
            // TODO: This should really be placed before the Metadata object is created,
            // but for now this ensures it is called regardless of which client is in use
            PluginHooks.LoadPipelineStarting();

            var metadata = new Metadata(statusCallback);
            stream.Position = 0;
            stream.CopyTo(metadata);
            metadata.Position = 0;
            metadata.Initialize();
            return metadata;
        }

        private Metadata(EventHandler<string> statusCallback = null) : base() => OnStatusUpdate = statusCallback;

        private void Initialize()
        {
            // Pre-processing hook
            var pluginResult = PluginHooks.PreProcessMetadata(this);
            IsModified = pluginResult.IsStreamModified;

            StatusUpdate("Processing metadata");

            // Read metadata header
            Header = ReadVersionedObject<Il2CppGlobalMetadataHeader>(0);

            // Check for correct magic bytes
            if (!Header.SanityValid) {
                throw new InvalidOperationException("The supplied metadata file is not valid.");
            }

            // Set object versioning for Bin2Object from metadata version
            Version = new StructVersion(Header.Version);

            if (Version < MetadataVersions.V160 || Version > MetadataVersions.V350) {
                throw new InvalidOperationException($"The supplied metadata file is not of a supported version ({Header.Version}).");
            }

            // Rewind and read metadata header with the correct version settings
            Header = ReadVersionedObject<Il2CppGlobalMetadataHeader>(0);

            // Sanity checking
            // Unity.IL2CPP.MetadataCacheWriter.WriteLibIl2CppMetadata always writes the metadata information in the same order it appears in the header,
            // with each block always coming directly after the previous block, 4-byte aligned. We can use this to check the integrity of the data and
            // detect sub-versions.

            // For metadata v24.0, the header can either be either 0x110 (24.0, 24.1) or 0x108 (24.2) bytes long. Since 'stringLiteralOffset' is the first thing
            // in the header after the sanity and version fields, and since it will always point directly to the first byte after the end of the header,
            // we can use this value to determine the actual header length and therefore narrow down the metadata version to 24.0/24.1 or 24.2.

            if (!pluginResult.SkipValidation) {
                var realHeaderLength = Header.StringLiteralOffset;

                if (realHeaderLength != Sizeof<Il2CppGlobalMetadataHeader>()) {
                    if (Version == MetadataVersions.V240) {
                        Version = MetadataVersions.V242;
                        Header = ReadVersionedObject<Il2CppGlobalMetadataHeader>(0);
                    }
                }

                if (realHeaderLength != Sizeof<Il2CppGlobalMetadataHeader>()) {
                    throw new InvalidOperationException("Could not verify the integrity of the metadata file or accurately identify the metadata sub-version");
                }
            }
            
            // Load all the relevant metadata using offsets provided in the header
            if (Version >= MetadataVersions.V160)
                Images = ReadVersionedObjectArray<Il2CppImageDefinition>(Header.ImagesOffset,  Header.ImagesSize / Sizeof<Il2CppImageDefinition>());

            // As an additional sanity check, all images in the metadata should have Mono.Cecil.MetadataToken == 1
            // In metadata v24.1, two extra fields were added which will cause the below test to fail.
            // In that case, we can then adjust the version number and reload
            // Tokens were introduced in v19 - we don't bother testing earlier versions
            if (Version >= MetadataVersions.V190 && Images.Any(x => x.Token != 1))
                if (Version == MetadataVersions.V240) {
                    Version = MetadataVersions.V241;

                    // No need to re-read the header, it's the same for both sub-versions
                    Images = ReadVersionedObjectArray<Il2CppImageDefinition>(Header.ImagesOffset, Header.ImagesSize / Sizeof<Il2CppImageDefinition>());

                    if (Images.Any(x => x.Token != 1))
                        throw new InvalidOperationException("Could not verify the integrity of the metadata file image list");
                }

            Types = ReadVersionedObjectArray<Il2CppTypeDefinition>(Header.TypeDefinitionsOffset, Header.TypeDefinitionsSize / Sizeof<Il2CppTypeDefinition>());
            Methods = ReadVersionedObjectArray<Il2CppMethodDefinition>(Header.MethodsOffset, Header.MethodsSize / Sizeof<Il2CppMethodDefinition>());
            Params = ReadVersionedObjectArray<Il2CppParameterDefinition>(Header.ParametersOffset, Header.ParametersSize / Sizeof<Il2CppParameterDefinition>());
            Fields = ReadVersionedObjectArray<Il2CppFieldDefinition>(Header.FieldsOffset, Header.FieldsSize / Sizeof<Il2CppFieldDefinition>());
            FieldDefaultValues = ReadVersionedObjectArray<Il2CppFieldDefaultValue>(Header.FieldDefaultValuesOffset, Header.FieldDefaultValuesSize / Sizeof<Il2CppFieldDefaultValue>());
            Properties = ReadVersionedObjectArray<Il2CppPropertyDefinition>(Header.PropertiesOffset, Header.PropertiesSize / Sizeof<Il2CppPropertyDefinition>());
            Events = ReadVersionedObjectArray<Il2CppEventDefinition>(Header.EventsOffset, Header.EventsSize / Sizeof<Il2CppEventDefinition>());
            InterfaceUsageIndices = ReadPrimitiveArray<int>(Header.InterfacesOffset, Header.InterfacesSize / sizeof(int));
            NestedTypeIndices = ReadPrimitiveArray<int>(Header.NestedTypesOffset, Header.NestedTypesSize / sizeof(int));
            GenericContainers = ReadVersionedObjectArray<Il2CppGenericContainer>(Header.GenericContainersOffset, Header.GenericContainersSize / Sizeof<Il2CppGenericContainer>());
            GenericParameters = ReadVersionedObjectArray<Il2CppGenericParameter>(Header.GenericParametersOffset, Header.GenericParametersSize / Sizeof<Il2CppGenericParameter>());
            GenericConstraintIndices = ReadPrimitiveArray<int>(Header.GenericParameterConstraintsOffset, Header.GenericParameterConstraintsSize / sizeof(int));
            InterfaceOffsets = ReadVersionedObjectArray<Il2CppInterfaceOffsetPair>(Header.InterfaceOffsetsOffset, Header.InterfaceOffsetsSize / Sizeof<Il2CppInterfaceOffsetPair>());
            VTableMethodIndices = ReadPrimitiveArray<uint>(Header.VTableMethodsOffset, Header.VTableMethodsSize / sizeof(uint));

            if (Version >= MetadataVersions.V160) {
                // In v24.4 hashValueIndex was removed from Il2CppAssemblyNameDefinition, which is a field in Il2CppAssemblyDefinition
                // The number of images and assemblies should be the same. If they are not, we deduce that we are using v24.4
                // Note the version comparison matches both 24.2 and 24.3 here since 24.3 is tested for during binary loading
                var assemblyCount = Header.AssembliesSize / Sizeof<Il2CppAssemblyDefinition>();
                var changedAssemblyDefStruct = false;
                if ((Version == MetadataVersions.V241 || Version == MetadataVersions.V242 || Version == MetadataVersions.V243) && assemblyCount < Images.Length)
                {
                    if (Version == MetadataVersions.V241)
                        changedAssemblyDefStruct = true;
                    Version = MetadataVersions.V244;
                }

                Assemblies = ReadVersionedObjectArray<Il2CppAssemblyDefinition>(Header.AssembliesOffset, Images.Length);

                if (changedAssemblyDefStruct)
                    Version = MetadataVersions.V241;

                ParameterDefaultValues = ReadVersionedObjectArray<Il2CppParameterDefaultValue>(Header.ParameterDefaultValuesOffset, Header.ParameterDefaultValuesSize / Sizeof<Il2CppParameterDefaultValue>());
            }
            if (Version >= MetadataVersions.V190 && Version < MetadataVersions.V270) {
                MetadataUsageLists = ReadVersionedObjectArray<Il2CppMetadataUsageList>(Header.MetadataUsageListsOffset, Header.MetadataUsageListsCount / Sizeof<Il2CppMetadataUsageList>());
                MetadataUsagePairs = ReadVersionedObjectArray<Il2CppMetadataUsagePair>(Header.MetadataUsagePairsOffset, Header.MetadataUsagePairsCount / Sizeof<Il2CppMetadataUsagePair>());
            }
            if (Version >= MetadataVersions.V190) {
                FieldRefs = ReadVersionedObjectArray<Il2CppFieldRef>(Header.FieldRefsOffset, Header.FieldRefsSize / Sizeof<Il2CppFieldRef>());
            }
            if (Version >= MetadataVersions.V210 && Version < MetadataVersions.V290) {
                AttributeTypeIndices = ReadPrimitiveArray<int>(Header.AttributesTypesOffset, Header.AttributesTypesCount / sizeof(int));
                AttributeTypeRanges = ReadVersionedObjectArray<Il2CppCustomAttributeTypeRange>(Header.AttributesInfoOffset, Header.AttributesInfoCount / Sizeof<Il2CppCustomAttributeTypeRange>());
            }

            if (Version >= MetadataVersions.V290)
            {
                AttributeDataRanges = ReadVersionedObjectArray<Il2CppCustomAttributeDataRange>(Header.AttributeDataRangeOffset,
                    Header.AttributeDataRangeSize / Sizeof<Il2CppCustomAttributeDataRange>());
            }

            // Get all metadata strings
            var pluginGetStringsResult = PluginHooks.GetStrings(this);
            if (pluginGetStringsResult.IsDataModified && !pluginGetStringsResult.IsInvalid)
                Strings = pluginGetStringsResult.Strings;

            else {
                Position = Header.StringOffset;

                while (Position < Header.StringOffset + Header.StringSize)
                    Strings.Add((int) Position - Header.StringOffset, ReadNullTerminatedString());
            }

            // Get all string literals
            var pluginGetStringLiteralsResult = PluginHooks.GetStringLiterals(this);
            if (pluginGetStringLiteralsResult.IsDataModified)
                StringLiterals = pluginGetStringLiteralsResult.StringLiterals.ToArray();

            else {
                var stringLiteralList = ReadVersionedObjectArray<Il2CppStringLiteral>(Header.StringLiteralOffset, Header.StringLiteralSize / Sizeof<Il2CppStringLiteral>());

                if (Version >= MetadataVersions.V350)
                {
                    StringLiterals = new string[stringLiteralList.Length - 1];
                    for (var i = 0; i < stringLiteralList.Length; i++)
                    {
                        var currentStringDataIndex = stringLiteralList[i].DataIndex;
                        var nextStringDataIndex = stringLiteralList[i + 1].DataIndex;
                        var stringLength = nextStringDataIndex - currentStringDataIndex;

                        StringLiterals[i] = ReadFixedLengthString(Header.StringLiteralDataOffset + currentStringDataIndex, stringLength);
                    }

                }
                else
                {
                    StringLiterals = new string[stringLiteralList.Length];
                    for (var i = 0; i < stringLiteralList.Length; i++)
                        StringLiterals[i] = ReadFixedLengthString(Header.StringLiteralDataOffset + stringLiteralList[i].DataIndex, (int)stringLiteralList[i].Length);

                }
            }

            // Post-processing hook
            IsModified |= PluginHooks.PostProcessMetadata(this).IsStreamModified;
        }

        // Save metadata to file, overwriting if necessary
        public void SaveToFile(string pathname) {
            Position = 0;
            using (var outFile = new FileStream(pathname, FileMode.Create, FileAccess.Write))
                CopyTo(outFile);
        }

        public int Sizeof<T>() where T : IReadable => T.Size(Version, Is32Bit);
    }
}
