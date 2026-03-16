using System.Runtime.InteropServices;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

using ImageIndex = int;

[VersionedStruct]
[StructLayout(LayoutKind.Explicit)]
public partial record struct Il2CppAssemblyDefinition
{
    [FieldOffset(20)]
    [VersionCondition(LessThanOrEqual = "15.0")]
    public Il2CppAssemblyNameDefinition LegacyAname;

    [FieldOffset(0)]
    public ImageIndex ImageIndex;

    [FieldOffset(4)]
    [VersionCondition(GreaterThanOrEqual = "24.1")]
    public uint Token;

    [FieldOffset(20 + 52)]
    [VersionCondition(GreaterThanOrEqual = "38.0")]
    public uint ModuleToken;

    [FieldOffset(8)]
    [VersionCondition(LessThanOrEqual = "24.0")]
    public int CustomAttributeIndex;

    [FieldOffset(12)]
    [VersionCondition(GreaterThanOrEqual = "20.0")]
    public int ReferencedAssemblyStart;

    [FieldOffset(16)]
    [VersionCondition(GreaterThanOrEqual = "20.0")]
    public int ReferencedAssemblyCount;

    [FieldOffset(20)]
    public Il2CppAssemblyNameDefinition Aname;
}