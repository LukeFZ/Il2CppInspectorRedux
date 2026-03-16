using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using MethodIndex = int;

[VersionedStruct]
public partial record struct Il2CppGenericMethodIndices
{
    public MethodIndex MethodIndex;
    public MethodIndex InvokerIndex;

    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThanOrEqual = "27.1")]
    public MethodIndex AdjustorThunkIndex;
}