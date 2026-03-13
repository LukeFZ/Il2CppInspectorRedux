namespace VersionedSerialization.Generator.Utils;

public static class Constants
{
    private const string AttributeNamespace = "VersionedSerialization.Attributes";

    public const string VersionedStructAttribute = $"{AttributeNamespace}.{nameof(VersionedStructAttribute)}";
    public const string VersionConditionAttribute = $"{AttributeNamespace}.{nameof(VersionConditionAttribute)}";
    public const string NativeIntegerAttribute = $"{AttributeNamespace}.{nameof(NativeIntegerAttribute)}";

    public const string InlineArrayAttribute = "System.Runtime.CompilerServices.InlineArrayAttribute";

    public const string LessThan = nameof(LessThan);
    public const string GreaterThan = nameof(GreaterThan);
    public const string EqualTo = nameof(EqualTo);
    public const string LessThanOrEqual = nameof(LessThanOrEqual);
    public const string GreaterThanOrEqual = nameof(GreaterThanOrEqual);

    public const string IncludingTag = nameof(IncludingTag);
    public const string ExcludingTag = nameof(ExcludingTag);
}