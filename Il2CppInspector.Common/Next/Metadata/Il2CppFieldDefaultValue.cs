using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

[VersionedStruct]
public partial record struct Il2CppFieldDefaultValue
{
    public FieldIndex FieldIndex { get; private set; }
    public TypeIndex TypeIndex { get; private set; }
    public DefaultValueDataIndex DataIndex { get; private set; }
}