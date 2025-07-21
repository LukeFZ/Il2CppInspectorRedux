using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using InvokerMethod = Il2CppMethodPointer;

[VersionedStruct]
public partial record struct Il2CppCodeRegistration
{
    [NativeInteger]
    [VersionCondition(LessThan = "24.1")]
    public uint MethodPointersCount;

    [VersionCondition(LessThan = "24.1")]
    public Pointer<Il2CppMethodPointer> MethodPointers;

    [NativeInteger]
    public uint ReversePInvokeWrapperCount;

    public Pointer<Il2CppMethodPointer> ReversePInvokeWrappers;

    [NativeInteger]
    [VersionCondition(LessThan = "22.0")]
    public uint DelegateWrappersFromManagedToNativeCount;

    [VersionCondition(LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> DelegateWrappersFromManagedToNative;

    [NativeInteger]
    [VersionCondition(LessThan = "22.0")]
    public uint MarshalingFunctionsCount;

    [VersionCondition(LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> MarshalingFunctions;

    [NativeInteger]
    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public uint CcwMarshalingFunctionsCount;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> CcwMarshalingFunctions;

    [NativeInteger]
    public uint GenericMethodPointersCount;

    public Pointer<Il2CppMethodPointer> GenericMethodPointers;

    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThan = "27.1")]
    public Pointer<Il2CppMethodPointer> GenericAdjustorThunks;

    [NativeInteger]
    public uint InvokerPointersCount;

    public Pointer<InvokerMethod> InvokerPointers;

    [NativeInteger]
    [VersionCondition(LessThan = "24.5")]
    public int CustomAttributeCount;

    [VersionCondition(LessThan = "24.5")]
    public Pointer<Il2CppMethodPointer> CustomAttributeGenerators;

    [NativeInteger]
    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public int GuidCount;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public Pointer<Il2CppGuid> Guids;

    [NativeInteger]
    [VersionCondition(GreaterThan = "22.0", LessThan = "27.2")]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "")]
    [VersionCondition(EqualTo = "31.0", IncludingTag = "")]
    public int UnresolvedVirtualCallCount;

    [NativeInteger]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "2023"), VersionCondition(EqualTo = "31.0", IncludingTag = "2023")]
    [VersionCondition(GreaterThan = "35.0")]
    public uint UnresolvedIndirectCallCount; // UnresolvedVirtualCallCount pre 29.1

    [VersionCondition(GreaterThan = "22.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedVirtualCallPointers;

    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "2023"), VersionCondition(EqualTo = "31.0", IncludingTag = "2023")]
    [VersionCondition(GreaterThan = "35.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedInstanceCallWrappers;

    [VersionCondition(EqualTo = "29.0", IncludingTag = "2022"), VersionCondition(EqualTo = "31.0", IncludingTag = "2022")]
    [VersionCondition(EqualTo = "29.0", IncludingTag = "2023"), VersionCondition(EqualTo = "31.0", IncludingTag = "2023")]
    [VersionCondition(GreaterThan = "35.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedStaticCallPointers;

    [NativeInteger]
    [VersionCondition(GreaterThan = "23.0")]
    public uint InteropDataCount;

    [VersionCondition(GreaterThan = "23.0")]
    public Pointer<Il2CppInteropData> InteropData;

    [NativeInteger]
    [VersionCondition(GreaterThan = "24.3")]
    public uint WindowsRuntimeFactoryCount;

    [VersionCondition(GreaterThan = "24.3")]
    public Pointer<Il2CppWindowsRuntimeFactoryTableEntry> WindowsRuntimeFactoryTable;

    [NativeInteger]
    [VersionCondition(GreaterThan = "24.2")]
    public uint CodeGenModulesCount;

    [VersionCondition(GreaterThan = "24.2")]
    public Pointer<Pointer<Il2CppCodeGenModule>> CodeGenModules;
}