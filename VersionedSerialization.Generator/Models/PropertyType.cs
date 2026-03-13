using System;

namespace VersionedSerialization.Generator.Models;

public enum PropertyType
{
    Unsupported = -1,
    None,
    Boolean,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Int8,
    Int16,
    Int32,
    Int64,
    String,
    NativeInteger,
    UNativeInteger,
    Bytes
}

public static class PropertyTypeExtensions
{
    extension(PropertyType type)
    {
        public string GetTypeName()
            => type switch
            {
                PropertyType.Unsupported => nameof(PropertyType.Unsupported),
                PropertyType.None => nameof(PropertyType.None),
                PropertyType.UInt8 => nameof(Byte),
                PropertyType.Int8 => nameof(SByte),
                PropertyType.Boolean => nameof(PropertyType.Boolean),
                PropertyType.UInt16 => nameof(PropertyType.UInt16),
                PropertyType.UInt32 => nameof(PropertyType.UInt32),
                PropertyType.UInt64 => nameof(PropertyType.UInt64),
                PropertyType.Int16 => nameof(PropertyType.Int16),
                PropertyType.Int32 => nameof(PropertyType.Int32),
                PropertyType.Int64 => nameof(PropertyType.Int64),
                PropertyType.String => nameof(String),
                PropertyType.NativeInteger => "NativeInt",
                PropertyType.UNativeInteger => "NativeUInt",
                PropertyType.Bytes => "",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public bool IsSeperateMethod()
            => type switch
            {
                PropertyType.Boolean => true,
                PropertyType.String => true,
                PropertyType.NativeInteger => true,
                PropertyType.UNativeInteger => true,
                PropertyType.Bytes => true,
                _ => false
            };

        public bool IsUnsignedType()
            => type switch
            {
                PropertyType.UInt8 
                    or PropertyType.UInt16
                    or PropertyType.UInt32
                    or PropertyType.UInt64 => true,
                _ => false
            };

        public bool NeedsAssignmentToMember()
            => type switch
            {
                PropertyType.Bytes => false,
                _ => true
            };
    }
}