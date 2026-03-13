using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VersionedSerialization.Generator.Models;
using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator;

#pragma warning disable RS1038
[Generator]
#pragma warning restore RS1038
public sealed class ObjectSerializationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
            
        var valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(Constants.VersionedStructAttribute,
                static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                static (context, _) => (ContextClass: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
            .Combine(context.CompilationProvider)
            .Select(static (tuple, cancellationToken) => ParseSerializationInfo(tuple.Left.ContextClass, tuple.Left.SemanticModel, tuple.Right, cancellationToken))
            .WithTrackingName(nameof(ObjectSerializationGenerator));

        context.RegisterSourceOutput(valueProvider, EmitCode);
    }

    private static void EmitCode(SourceProductionContext sourceProductionContext, ObjectSerializationInfo info)
    {
        var generator = new CodeGenerator();
        generator.AppendLine("#nullable restore");
        generator.AppendLine("using VersionedSerialization;");
        generator.AppendLine("using System.Runtime.CompilerServices;");
        generator.AppendLine();

        generator.AppendLine($"namespace {info.Namespace};");

        var versions = new HashSet<StructVersion>();
        foreach (var condition in info.Properties.SelectMany(static x => x.VersionConditions))
        {
            if (condition.LessThan.HasValue)
                versions.Add(condition.LessThan.Value);

            if (condition.GreaterThan.HasValue)
                versions.Add(condition.GreaterThan.Value);

            if (condition.EqualTo.HasValue)
                versions.Add(condition.EqualTo.Value);

            if (condition.LessThanOrEqual.HasValue)
                versions.Add(condition.LessThanOrEqual.Value);

            if (condition.GreaterThanOrEqual.HasValue)
                versions.Add(condition.GreaterThanOrEqual.Value);
        }

        if (versions.Count > 0)
        {
            generator.EnterScope("file static class Versions");

            foreach (var version in versions)
            {
                generator.AppendLine($"public static readonly StructVersion {GetVersionIdentifier(version)} = \"{version}\";");
            }

            generator.LeaveScope();
        }

        var definitionType = info.DefinitionType switch
        {
            SyntaxKind.ClassDeclaration => "class",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.RecordStructDeclaration => "record struct",
            _ => throw new IndexOutOfRangeException()
        };

        var visibility = info.IsPublic ? "public" : "internal";

        generator.EnterScope($"{visibility} partial {definitionType} {info.Name} : IReadable");
        GenerateReadMethod(generator, info);
        generator.AppendLine();
        GenerateSizeMethod(generator, info);
        generator.LeaveScope();

        sourceProductionContext.AddSource($"{info.Namespace}.{info.Name}.g.cs", generator.ToString());
    }

    private static void GenerateSizeMethod(CodeGenerator generator, ObjectSerializationInfo info)
    {
        generator.EnterScope("static int IReadable.Size(in StructVersion version, in ReaderConfig config)");

        if (!info.CanGenerateSizeMethod)
        {
            generator.AppendLine("throw new InvalidOperationException(\"No size can be calculated for this struct.\");");
        }
        else
        {
            generator.AppendLine("var size = 0;");
            if (info.HasBaseType)
                generator.AppendLine("size += base.StructSize(in version, in config);");

            foreach (var property in info.Properties)
            {
                if (property.VersionConditions.Length > 0)
                    GenerateVersionCondition(property.VersionConditions, generator);

                generator.EnterScope();
                generator.AppendLine($"size += {property.SizeExpression};");
                generator.LeaveScope();
            }

            generator.AppendLine("return size;");
        }

        generator.LeaveScope();
    }

    private static void GenerateReadMethod(CodeGenerator generator, ObjectSerializationInfo info)
    {
        generator.EnterScope("public void Read<TReader>(ref Reader<TReader> reader, in StructVersion version = default) where TReader : IReader, allows ref struct");

        if (info.HasBaseType)
            generator.AppendLine("base.Read(ref reader, in version);");

        foreach (var property in info.Properties)
        {
            if (property.VersionConditions.Length > 0)
                GenerateVersionCondition(property.VersionConditions, generator);

            generator.EnterScope();
            generator.AppendLine(property.Type.NeedsAssignmentToMember()
                ? $"this.{property.Name} = {property.ReadMethod}"
                : property.ReadMethod);

            generator.LeaveScope();
        }

        generator.LeaveScope();
    }

    private static string GetVersionIdentifier(StructVersion version)
        => $"V{version.Major}_{version.Minor}{(version.Tag == null ? "" : $"_{version.Tag}")}";

    private static void GenerateVersionCondition(ImmutableEquatableArray<VersionCondition> conditions,
        CodeGenerator generator)
    {
        generator.AppendLine("if (");
        generator.IncreaseIndentation();

        for (var i = 0; i < conditions.Length; i++)
        {
            generator.AppendLine("(true");

            var condition = conditions[i];
            if (condition.LessThan.HasValue)
                generator.AppendLine($"&& version < Versions.{GetVersionIdentifier(condition.LessThan.Value)}");

            if (condition.GreaterThan.HasValue)
                generator.AppendLine($"&& version > Versions.{GetVersionIdentifier(condition.GreaterThan.Value)}");

            if (condition.EqualTo.HasValue)
                generator.AppendLine($"&& version == Versions.{GetVersionIdentifier(condition.EqualTo.Value)}");

            if (condition.LessThanOrEqual.HasValue)
                generator.AppendLine($"&& version <= Versions.{GetVersionIdentifier(condition.LessThanOrEqual.Value)}");

            if (condition.GreaterThanOrEqual.HasValue)
                generator.AppendLine($"&& version >= Versions.{GetVersionIdentifier(condition.GreaterThanOrEqual.Value)}");

            if (condition.IncludingTag != null)
                generator.AppendLine(condition.IncludingTag == ""
                    ? "&& version.Tag == null"
                    : $"&& version.Tag == \"{condition.IncludingTag}\"");

            if (condition.ExcludingTag != null)
                generator.AppendLine(condition.ExcludingTag == ""
                    ? "&& version.Tag != null"
                    : $"&& version.Tag != \"{condition.ExcludingTag}\"");

            generator.AppendLine(")");

            if (i != conditions.Length - 1)
                generator.AppendLine("||");
        }

        generator.DecreaseIndentation();
        generator.AppendLine(")");
    }

    private static ObjectSerializationInfo ParseSerializationInfo(TypeDeclarationSyntax contextClass,
        SemanticModel model, Compilation compilation,
        CancellationToken cancellationToken)
    {
        var classSymbol = model.GetDeclaredSymbol(contextClass, cancellationToken) ?? throw new InvalidOperationException();
        var classIsPublic = classSymbol.DeclaredAccessibility == Accessibility.Public;

        //var versionedStructAttribute = compilation.GetTypeByMetadataName(Constants.VersionedStructAttribute);
        var versionConditionAttribute = compilation.GetTypeByMetadataName(Constants.VersionConditionAttribute);
        var nativeIntegerAttribute = compilation.GetTypeByMetadataName(Constants.NativeIntegerAttribute);
        var inlineArrayAttribute = compilation.GetTypeByMetadataName(Constants.InlineArrayAttribute);

        var canGenerateSizeMethod = true;

        var properties = new List<PropertySerializationInfo>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member.IsStatic 
                || member is IFieldSymbol { AssociatedSymbol: not null } 
                || member is IPropertySymbol { SetMethod: null })
                continue;

            var versionConditions = new List<VersionCondition>();

            ITypeSymbol type;
            switch (member)
            {
                case IFieldSymbol field:
                    type = field.Type;
                    break;
                case IPropertySymbol property:
                    type = property.Type;
                    break;
                default:
                    continue;
            }

            var typeInfo = ParseType(type, inlineArrayAttribute);
            string? readMethod = null;
            string? sizeExpression = null;

            foreach (var attribute in member.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, versionConditionAttribute))
                {
                    StructVersion? lessThan = null,
                        moreThan = null,
                        equalTo = null,
                        lessThanOrEqual = null,
                        moreThanOrEqual = null;

                    string? includingTag = null,
                        excludingTag = null;

                    foreach (var argument in attribute.NamedArguments)
                    {
                        var stringArgument = (string)argument.Value.Value!;

                        switch (argument.Key)
                        {
                            case Constants.LessThan:
                                lessThan = stringArgument;
                                break;
                            case Constants.GreaterThan:
                                moreThan = stringArgument;
                                break;
                            case Constants.EqualTo:
                                equalTo = stringArgument;
                                break;
                            case Constants.LessThanOrEqual:
                                lessThanOrEqual = stringArgument;
                                break;
                            case Constants.GreaterThanOrEqual:
                                moreThanOrEqual = stringArgument;
                                break;
                            case Constants.IncludingTag:
                                includingTag = stringArgument;
                                break;
                            case Constants.ExcludingTag:
                                excludingTag = stringArgument;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    var condition = new VersionCondition(
                        lessThan,
                        moreThan,
                        equalTo,
                        lessThanOrEqual,
                        moreThanOrEqual,
                        includingTag,
                        excludingTag);

                    versionConditions.Add(condition);
                }
                else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, nativeIntegerAttribute))
                {
                    var nativeIntegerType = typeInfo.Type.IsUnsignedType()
                        ? PropertyType.UNativeInteger
                        : PropertyType.NativeInteger;

                    var complexTypeName = typeInfo.ComplexTypeName == ""
                        ? typeInfo.Type.GetTypeName()
                        : typeInfo.ComplexTypeName;

                    typeInfo = (nativeIntegerType, complexTypeName, typeInfo.IsArray);
                }
            }

            canGenerateSizeMethod &= typeInfo.Type != PropertyType.String;

            if (readMethod == null)
            {
                if (typeInfo.Type == PropertyType.None)
                {
                    readMethod = $"reader.ReadVersionedObject<{typeInfo.ComplexTypeName}>(in version);";
                }
                else if (typeInfo.Type == PropertyType.Bytes)
                {
                    readMethod = $"reader.Read<byte>({member.Name});";
                }
                else
                {
                    readMethod = typeInfo.Type.IsSeperateMethod()
                        ? $"reader.Read{typeInfo.Type.GetTypeName()}();"
                        : $"reader.ReadPrimitive<{typeInfo.Type.GetTypeName()}>();";

                    if (typeInfo.ComplexTypeName != "")
                        readMethod = $"({typeInfo.ComplexTypeName}){readMethod}";
                }
            }

            sizeExpression ??= typeInfo.Type switch
            {
                PropertyType.None => $"{typeInfo.ComplexTypeName}.StructSize(in version, in config)",
                PropertyType.NativeInteger or PropertyType.UNativeInteger =>
                    "config.Is32Bit ? sizeof(uint) : sizeof(ulong)",
                PropertyType.Bytes => $"Unsafe.SizeOf<{typeInfo.ComplexTypeName}>()",
                _ => $"sizeof({typeInfo.Type.GetTypeName()})"
            };

            properties.Add(new PropertySerializationInfo(
                member.Name,
                readMethod,
                sizeExpression,
                typeInfo.Type,
                versionConditions.ToImmutableEquatableArray()
            ));
        }

        var hasBaseType = false;
        if (classSymbol.BaseType != null)
        {
            var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
            var valueTypeSymbol = compilation.GetSpecialType(SpecialType.System_ValueType);

            if (!SymbolEqualityComparer.Default.Equals(objectSymbol, classSymbol.BaseType)
                && !SymbolEqualityComparer.Default.Equals(valueTypeSymbol, classSymbol.BaseType))
                hasBaseType = true;
        }

        return new ObjectSerializationInfo(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            hasBaseType,
            classIsPublic,
            contextClass.Kind(),
            canGenerateSizeMethod,
            properties.ToImmutableEquatableArray()
        );
    }

    private static (PropertyType Type, string ComplexTypeName, bool IsArray) ParseType(ITypeSymbol typeSymbol,
        INamedTypeSymbol? inlineArraySymbol)
    {
        switch (typeSymbol)
        {
            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var elementType = ParseType(arrayTypeSymbol.ElementType, inlineArraySymbol);
                return (elementType.Type, elementType.ComplexTypeName, true);
            }
            case INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol:
                var res = ParseType(namedTypeSymbol.EnumUnderlyingType, inlineArraySymbol);
                return (res.Type, typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), false);
        }

        if (typeSymbol.SpecialType != SpecialType.None)
        {
            var type = typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => PropertyType.Boolean,
                SpecialType.System_Byte => PropertyType.UInt8,
                SpecialType.System_UInt16 => PropertyType.UInt16,
                SpecialType.System_UInt32 => PropertyType.UInt32,
                SpecialType.System_UInt64 => PropertyType.UInt64,
                SpecialType.System_SByte => PropertyType.Int8,
                SpecialType.System_Int16 => PropertyType.Int16,
                SpecialType.System_Int32 => PropertyType.Int32,
                SpecialType.System_Int64 => PropertyType.Int64,
                SpecialType.System_String => PropertyType.String,
                _ => PropertyType.Unsupported
            };

            return (type, "", false);
        }

        var complexType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, inlineArraySymbol))
                return (PropertyType.Bytes, complexType, false);
        }

        return (PropertyType.None, complexType, false);
    }
}