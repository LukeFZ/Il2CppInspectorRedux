namespace Il2CppInspector.Next.Metadata;

using StringIndex = int;
using AssemblyIndex = int;
using CustomAttributeIndex = int;
using VersionedSerialization.Attributes;

[VersionedStruct]
public partial record struct Il2CppImageDefinition
{
    public StringIndex NameIndex { get; private set; }
    public AssemblyIndex AssemblyIndex { get; private set; }

    public TypeDefinitionIndex TypeStart { get; private set; }
    public uint TypeCount { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "24.0")]
    public TypeDefinitionIndex ExportedTypeStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "24.0")]
    public uint ExportedTypeCount { get; private set; }

    public MethodIndex EntryPointIndex { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "19.0")]
    public uint Token { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "24.1")]
    public CustomAttributeIndex CustomAttributeStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "24.1")]
    public uint CustomAttributeCount { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "108.0")]
    public int InvokerIndicesStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "108.0")]
    public int RgctxRangesStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "108.0")]
    public int RgctxRangesCount { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "108.0")]
    public TypeDefinitionIndex StaticConstructorStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "108.0")]
    public int StaticConstructorCount { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "110.0")]
    public int FieldStart { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "110.0")]
    public int PropertyIndex { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "110.0")]
    public int EventIndex { get; private set; }

    [VersionCondition(GreaterThanOrEqual = "110.0")]
    public MethodIndex MethodStart { get; private set; }

}