namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;
using StringIndex = int;

[VersionedStruct]
public partial record struct Il2CppFieldDefinition
{
    public StringIndex NameIndex { get; private set; }
    public TypeIndex TypeIndex { get; private set; }

    [VersionCondition(LessThanOrEqual = "24.0")]
    public int CustomAttributeIndex { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "19.0")]
    public uint Token { get; private set; }

    public readonly bool IsValid => NameIndex != 0;
}