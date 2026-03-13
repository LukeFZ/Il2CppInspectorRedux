namespace VersionedSerialization.Generator.Models;

public sealed record VersionCondition(
    StructVersion? LessThan, 
    StructVersion? GreaterThan, 
    StructVersion? EqualTo,
    StructVersion? LessThanOrEqual,
    StructVersion? GreaterThanOrEqual,
    string? IncludingTag, 
    string? ExcludingTag
);