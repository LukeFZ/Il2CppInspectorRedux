using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using InvokerMethod = Il2CppMethodPointer;

[VersionedStruct]
public partial record struct Il2CppCodeRegistration
{
    [NativeInteger]
    [VersionCondition(LessThanOrEqual = "24.1")]
    public uint MethodPointersCount;

    [VersionCondition(LessThanOrEqual = "24.1")]
    public Pointer<Il2CppMethodPointer> MethodPointers;

    [NativeInteger]
    public uint ReversePInvokeWrapperCount;

    public Pointer<Il2CppMethodPointer> ReversePInvokeWrappers;

    [NativeInteger]
    [VersionCondition(LessThanOrEqual = "22.0")]
    public uint DelegateWrappersFromManagedToNativeCount;

    [VersionCondition(LessThanOrEqual = "22.0")]
    public Pointer<Il2CppMethodPointer> DelegateWrappersFromManagedToNative;

    [NativeInteger]
    [VersionCondition(LessThanOrEqual = "22.0")]
    public uint MarshalingFunctionsCount;

    [VersionCondition(LessThanOrEqual = "22.0")]
    public Pointer<Il2CppMethodPointer> MarshalingFunctions;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "21.0", LessThanOrEqual = "22.0")]
    public uint CcwMarshalingFunctionsCount;

    [VersionCondition(GreaterThanOrEqual = "21.0", LessThanOrEqual = "22.0")]
    public Pointer<Il2CppMethodPointer> CcwMarshalingFunctions;

    [NativeInteger]
    public uint GenericMethodPointersCount;

    public Pointer<Il2CppMethodPointer> GenericMethodPointers;

    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThanOrEqual = "27.1")]
    public Pointer<Il2CppMethodPointer> GenericAdjustorThunks;

    [NativeInteger]
    public uint InvokerPointersCount;

    public Pointer<InvokerMethod> InvokerPointers;

    [NativeInteger]
    [VersionCondition(LessThanOrEqual = "24.5")]
    public int CustomAttributeCount;

    [VersionCondition(LessThanOrEqual = "24.5")]
    public Pointer<Il2CppMethodPointer> CustomAttributeGenerators;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "21.0", LessThanOrEqual = "22.0")]
    public int GuidCount;

    [VersionCondition(GreaterThanOrEqual = "21.0", LessThanOrEqual = "22.0")]
    public Pointer<Il2CppGuid> Guids;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "22.0", LessThanOrEqual = "27.2")]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "")]
    [VersionCondition(EqualTo = "31.0", IncludingTag = "")]
    public int UnresolvedVirtualCallCount;

    [NativeInteger]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(GreaterThanOrEqual = "35.0")]
    public uint UnresolvedIndirectCallCount; // UnresolvedVirtualCallCount pre 29.1

    [VersionCondition(GreaterThanOrEqual = "22.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedVirtualCallPointers;

    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(GreaterThanOrEqual = "35.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedInstanceCallWrappers;

    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(GreaterThanOrEqual = "35.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedStaticCallPointers;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "23.0")]
    public uint InteropDataCount;

    [VersionCondition(GreaterThanOrEqual = "23.0")]
    public Pointer<Il2CppInteropData> InteropData;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "24.3")]
    public uint WindowsRuntimeFactoryCount;

    [VersionCondition(GreaterThanOrEqual = "24.3")]
    public Pointer<Il2CppWindowsRuntimeFactoryTableEntry> WindowsRuntimeFactoryTable;

    [NativeInteger]
    [VersionCondition(GreaterThanOrEqual = "24.2")]
    public uint CodeGenModulesCount;

    [VersionCondition(GreaterThanOrEqual = "24.2")]
    public Pointer<Pointer<Il2CppCodeGenModule>> CodeGenModules;
}