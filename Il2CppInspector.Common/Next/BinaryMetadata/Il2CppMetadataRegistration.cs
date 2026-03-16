using Il2CppInspector.Next.Metadata;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using FieldIndex = int;
using TypeDefinitionIndex = int;

[VersionedStruct]
public partial record struct Il2CppMetadataRegistration
{
    [NativeInteger]
    public int GenericClassesCount;

    public Pointer<Pointer<Il2CppGenericClass>> GenericClasses;

    [NativeInteger]
    public int GenericInstsCount;

    public Pointer<Pointer<Il2CppGenericInst>> GenericInsts;

    [NativeInteger]
    public int GenericMethodTableCount;

    public Pointer<Il2CppGenericMethodFunctionsDefinitions> GenericMethodTable;

    [NativeInteger]
    public int TypesCount;

    public Pointer<Pointer<Il2CppType>> Types;

    [NativeInteger]
    public int MethodSpecsCount;

    public Pointer<Il2CppMethodSpec> MethodSpecs;

    [NativeInteger]
    [VersionCondition(LessThanOrEqual = "16.0")]
    public int MethodReferencesCount;

    [VersionCondition(LessThanOrEqual = "16.0")]
    public PrimitivePointer<PrimitivePointer<uint>> MethodReferences; // uint**

    [NativeInteger]
    public FieldIndex FieldOffsetsCount;

    public PrimitivePointer<PrimitivePointer<int>> FieldOffsets; // int**

    [NativeInteger]
    public TypeDefinitionIndex TypeDefinitionsSizesCount;
    public Pointer<Pointer<Il2CppTypeDefinitionSizes>> TypeDefinitionsSizes;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "19.0")]
    public ulong MetadataUsagesCount;

    [VersionCondition(GreaterThanOrEqual = "19.0")]
    public Pointer<Pointer<Il2CppMetadataUsage>> MetadataUsages;
}