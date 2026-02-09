using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

[VersionedStruct]
public partial record struct Il2CppFieldRef
{
    public TypeIndex TypeIndex { get; private set; }
    public FieldIndex FieldIndex { get; private set; }
}