using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

[VersionedStruct]
public partial struct Il2CppGeneratedMethodTypeInfo
{
    public int TypeIndex { get; private set; }
    public int GeneratedMethodStart { get; private set; }
    public int GeneratedMethodCount { get; private set; }
}