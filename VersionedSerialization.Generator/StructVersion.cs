using System;

namespace VersionedSerialization.Generator;

public readonly record struct StructVersion(int Major = 0, int Minor = 0, string? Tag = null)
{
    #region Equality operators

    public static bool operator >(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor > right.Minor);

    public static bool operator <(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor < right.Minor);

    public static bool operator >=(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor >= right.Minor);

    public static bool operator <=(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor <= right.Minor);

    #endregion

    public override string ToString() => $"{Major}.{Minor}{(Tag != null ? $"-{Tag}" : "")}";

    public static bool TryParse(string version, out StructVersion parsed)
    {
        parsed = default;

        var versionParts = version.Split('.');
        if (versionParts.Length is 1 or > 2)
            return false;

        var tagParts = versionParts[1].Split('-');
        if (tagParts.Length > 2)
            return false;

        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(tagParts[0]);
        var tag = tagParts.Length == 1 ? null : tagParts[1];

        parsed = new StructVersion(major, minor, tag);
        return true;
    }

    public static implicit operator StructVersion(string value)
    {
        if (!TryParse(value, out var ver))
            throw new InvalidOperationException("Invalid version string");

        return ver;
    }
}