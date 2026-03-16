namespace Il2CppInspector.Next.Metadata;

using StringIndex = int;
using VersionedSerialization.Attributes;

[VersionedStruct]
public partial record struct Il2CppEventDefinition
{
    public StringIndex NameIndex { get; private set; }
    public TypeIndex TypeIndex { get; private set; }
    public MethodIndex Add { get; private set; }
    public MethodIndex Remove { get; private set; }
    public MethodIndex Raise { get; private set; }

    [VersionCondition(LessThanOrEqual = "24.0")]
    public int CustomAttributeIndex { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "19.0")]
    public uint Token { get; private set; }

    public readonly bool IsValid => NameIndex != 0;
}