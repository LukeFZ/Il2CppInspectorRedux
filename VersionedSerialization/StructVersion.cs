using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace VersionedSerialization;

public readonly struct StructVersion(int major = 0, int minor = 0, string? tag = null)
    : IEquatable<StructVersion>, IParsable<StructVersion>
{
    public readonly int Major = major;
    public readonly int Minor = minor;
    public readonly string? Tag = tag;

    public double AsDouble => Major + Minor / 10.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag)
        => Tag != null && Tag.Contains(tag, StringComparison.Ordinal);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out StructVersion result)
    {
        if (s == null)
        {
            result = default;
            return false;
        }

        var versionParts = s.Split('.');
        if (versionParts.Length > 2)
        {
            result = default;
            return false;
        }

        if (versionParts.Length == 1)
        {
            if (!int.TryParse(versionParts[0], out var version))
            {
                result = default;
                return false;
            }

            result = new StructVersion(version);
            return true;
        }

        var tagParts = versionParts[1].Split("-");
        if (tagParts.Length > 2)
        {
            result = default;
            return false;
        }

        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(tagParts[0]);
        var tag = tagParts.ElementAtOrDefault(1);

        result = new StructVersion(major, minor, tag);
        return true;
    }

    public static StructVersion Parse(string s, IFormatProvider? provider = null) =>
        TryParse(s, provider, out var version) 
            ? version
            : throw new InvalidOperationException($"Failed to parse {s} as a StructVersion.");

    public static implicit operator StructVersion(string value)
        => Parse(value);

    public static implicit operator StructVersion(double value)
        => new((int)value, (int)((value - (int)value) * 10.0));

    #region Equality operators

    public static bool operator ==(StructVersion left, StructVersion right)
        => left.Major == right.Major && left.Minor == right.Minor;

    public static bool operator !=(StructVersion left, StructVersion right)
        => !(left == right);

    public static bool operator >(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor > right.Minor);

    public static bool operator <(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor < right.Minor);

    public static bool operator >=(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor >= right.Minor);

    public static bool operator <=(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor <= right.Minor);

    public override bool Equals(object? obj)
        => obj is StructVersion other && Equals(other);

    public bool Equals(StructVersion other)
        => Major == other.Major && Minor == other.Minor;

    public override int GetHashCode()
        => HashCode.Combine(Major, Minor);

    #endregion

    public override string ToString() => $"{Major}.{Minor}{(Tag != null ? $"-{Tag}" : "")}";
}