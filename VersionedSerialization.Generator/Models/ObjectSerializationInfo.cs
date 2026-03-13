using Microsoft.CodeAnalysis.CSharp;
using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator.Models;

public sealed record ObjectSerializationInfo(
    string Namespace,
    string Name,
    bool HasBaseType,
    bool IsPublic,
    SyntaxKind DefinitionType,
    bool CanGenerateSizeMethod,
    ImmutableEquatableArray<PropertySerializationInfo> Properties
);