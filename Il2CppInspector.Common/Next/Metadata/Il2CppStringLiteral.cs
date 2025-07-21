namespace Il2CppInspector.Next.Metadata;

using VersionedSerialization.Attributes;
using StringLiteralIndex = int;

[VersionedStruct]
public partial record struct Il2CppStringLiteral
{
    [VersionCondition(LessThan = "31.0")]
    public uint Length { get; private set; }
    public StringLiteralIndex DataIndex { get; private set; }
}