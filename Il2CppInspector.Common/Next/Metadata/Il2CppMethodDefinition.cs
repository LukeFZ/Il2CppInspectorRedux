using System.Reflection;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.Metadata;

using StringIndex = int;
using TypeDefinitionIndex = int;
using TypeIndex = int;
using ParameterIndex = int;
using GenericContainerIndex = int;

[VersionedStruct]
public partial record struct Il2CppMethodDefinition
{
    public StringIndex NameIndex { get; private set; }

    [VersionCondition(GreaterThan = "16.0")]
    public TypeDefinitionIndex DeclaringType { get; private set; }
    public TypeIndex ReturnType { get; private set; }

    [VersionCondition(GreaterThan = "31.0")]
    public uint ReturnParameterToken { get; private set; }

    public ParameterIndex ParameterStart { get; private set; }

    [VersionCondition(LessThan = "24.0")]
    public int CustomAttributeIndex { get; private set; }

    public GenericContainerIndex GenericContainerIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int MethodIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int InvokerIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int ReversePInvokeWrapperIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int RgctxStartIndex { get; private set; }

    [VersionCondition(LessThan = "24.1")]
    public int RgctxCount { get; private set; }

    public uint Token { get; private set; }
    public ushort Flags { get; private set; }
    public ushort ImplFlags { get; private set; }
    public ushort Slot { get; private set; }
    public ushort ParameterCount { get; private set; }

    public readonly MethodAttributes Attributes => (MethodAttributes)Flags;
    public readonly MethodImplAttributes ImplAttributes => (MethodImplAttributes)ImplFlags;

    public readonly bool IsValid => NameIndex != 0;
}