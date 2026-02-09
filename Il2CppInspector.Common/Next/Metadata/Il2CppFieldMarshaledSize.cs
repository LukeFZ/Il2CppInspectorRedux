namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;

[VersionedStruct]
public partial record struct Il2CppFieldMarshaledSize
{
    public FieldIndex FieldIndex { get; private set; }
    public TypeIndex TypeIndex { get; private set; }
    public int MarshaledSize { get; private set; }
}