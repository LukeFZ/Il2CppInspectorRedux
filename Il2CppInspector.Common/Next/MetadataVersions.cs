using VersionedSerialization;

namespace Il2CppInspector.Next;

public static class MetadataVersions
{
    public static readonly StructVersion V160 = new(16);

    public static readonly StructVersion V190 = new(19);

    public static readonly StructVersion V210 = new(21);
    public static readonly StructVersion V220 = new(22);

    public static readonly StructVersion V240 = new(24);
    public static readonly StructVersion V241 = new(24, 1);
    public static readonly StructVersion V242 = new(24, 2);
    public static readonly StructVersion V243 = new(24, 3);
    public static readonly StructVersion V244 = new(24, 4);
    public static readonly StructVersion V245 = new(24, 5);

    public static readonly StructVersion V270 = new(27);
    public static readonly StructVersion V271 = new(27, 1);
    public static readonly StructVersion V272 = new(27, 2);

    // These two versions have two variations:
    public static readonly StructVersion V290 = new(29);
    public static readonly StructVersion V310 = new(31);

    // No tag - 29.0/31.0
    public const string Tag2022 = "2022"; // 29.1/31.1

    // Unity 6000.3.0a2
    public static readonly StructVersion V350 = new(35);

    // Unity 6000.3.0a5
    public static readonly StructVersion V380 = new(38);
    // NOTE: This version uses tags to specify the size of TypeIndex, TypeDefinitionIndex, and GenericContainerIndex.

    // Unity 6000.3.0b1
    public static readonly StructVersion V390 = new(39);
    // NOTE: This version additionally uses a tag to specify the size of ParameterIndex.

    // Unity 6000.5.0a3
    public static readonly StructVersion V1040 = new(104);
    // NOTE: This version additionally uses tags to specify the size of InterfaceIndex, EventIndex, PropertyIndex, NestedTypeIndex,
    // alongside a new metadata section for Il2CppInlineArrayLength and a bitfield flag indicating an inline array.

    // Unity 6000.5.0a5
    public static readonly StructVersion V1050 = new(105);
    // NOTE: This version additionally uses a tag to specify the size of MethodIndex.

    // Unity 6000.5.0a6
    public static readonly StructVersion V1060 = new(106);
    // NOTE: This version additionally uses tags to specify the size of GenericParameterIndex, FieldIndex, DefaultValueDataIndex,
    // alongside changing TypeArgc to a ushort and IsMethod to a byte in Il2CppGenericContainer.
}