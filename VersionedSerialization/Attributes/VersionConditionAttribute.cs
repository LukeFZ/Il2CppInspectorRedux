namespace VersionedSerialization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class VersionConditionAttribute : Attribute
{
    public string LessThan { get; set; } = "";
    public string GreaterThan { get; set; } = "";
    public string EqualTo { get; set; } = "";

    public string LessThanOrEqual { get; set; } = "";
    public string GreaterThanOrEqual { get; set; } = "";

    public string IncludingTag { get; set; } = "";
    public string ExcludingTag { get; set; } = "";
}