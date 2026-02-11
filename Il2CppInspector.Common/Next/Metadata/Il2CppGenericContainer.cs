using System.Runtime.InteropServices;

namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;

[VersionedStruct]
[StructLayout(LayoutKind.Explicit)]
public partial record struct Il2CppGenericContainer
{
    [field: FieldOffset(0)]
    public int OwnerIndex { get; private set; }

    [VersionCondition(LessThan = "105.0")]
    [field: FieldOffset(4)]
    public int TypeArgc { get; private set; }

    [VersionCondition(LessThan = "105.0")]
    [field: FieldOffset(8)]
    public int IsMethod { get; private set; }

    [field: FieldOffset(12)]
    public GenericParameterIndex GenericParameterStart { get; private set; }

    [VersionCondition(GreaterThan = "106.0")] 
    [field: FieldOffset(4)]
    private ushort _newTypeArgc;

    [VersionCondition(GreaterThan = "106.0")] 
    [field: FieldOffset(8)]
    public byte _newIsMethod;
}