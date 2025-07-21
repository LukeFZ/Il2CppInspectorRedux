using System.Reflection;
using VersionedSerialization;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

using StringIndex = int;
using TypeIndex = int;
using GenericContainerIndex = int;
using FieldIndex = int;
using MethodIndex = int;
using EventIndex = int;
using PropertyIndex = int;
using NestedTypeIndex = int;
using InterfacesIndex = int;
using VTableIndex = int;

[VersionedStruct]
public partial record struct Il2CppTypeDefinition
{
    public const TypeIndex InvalidTypeIndex = -1;

    public StringIndex NameIndex { get; private set; }
    public StringIndex NamespaceIndex { get; private set; }

    [VersionCondition(LessThan = "24.0")]
    public int CustomAttributeIndex { get; private set; }

    public TypeIndex ByValTypeIndex { get; private set; }

    [VersionCondition(LessThan = "24.5")]
    public TypeIndex ByRefTypeIndex { get; private set; }

    public TypeIndex DeclaringTypeIndex { get; private set; }
    public TypeIndex ParentIndex { get; private set; }

    [VersionCondition(LessThan = "31.0")]
    public TypeIndex ElementTypeIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int RgctxStartIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int RgctxCount { get; private set; }

    public GenericContainerIndex GenericContainerIndex { get; private set; }

    [VersionCondition(LessThan = "22.0")]
    public int ReversePInvokeWrapperIndex { get; private set; }

    [VersionCondition(LessThan = "22.0")]
    public int MarshalingFunctionsIndex { get; private set; }

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public int CcwFunctionIndex { get; private set; }

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public int GuidIndex { get; private set; }

    public TypeAttributes Flags { get; private set; }

    public FieldIndex FieldIndex { get; private set; }
    public MethodIndex MethodIndex { get; private set; }
    public EventIndex EventIndex { get; private set; }
    public PropertyIndex PropertyIndex { get; private set; }
    public NestedTypeIndex NestedTypeIndex { get; private set; }
    public InterfacesIndex InterfacesIndex { get; private set; }
    public VTableIndex VTableIndex { get; private set; }
    public InterfacesIndex InterfaceOffsetsStart { get; private set; }

    public ushort MethodCount { get; private set; }
    public ushort PropertyCount { get; private set; }
    public ushort FieldCount { get; private set; }
    public ushort EventCount { get; private set; }
    public ushort NestedTypeCount { get; private set; }
    public ushort VTableCount { get; private set; }
    public ushort InterfacesCount { get; private set; }
    public ushort InterfaceOffsetsCount { get; private set; }

    public Il2CppTypeDefinitionBitfield Bitfield { get; private set; }

    [VersionCondition(GreaterThan = "19.0")]
    public uint Token { get; private set; }

    public readonly bool IsValid => NameIndex != 0;

    public int GetEnumElementTypeIndex(StructVersion version)
        => version >= MetadataVersions.V350
            ? ParentIndex
            : ElementTypeIndex;
}