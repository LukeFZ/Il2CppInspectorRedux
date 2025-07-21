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
    public static readonly string Tag2022 = "2022"; // 29.1/31.1

    // Unity 6000.3.0a2
    public static readonly StructVersion V350 = new(35);
}