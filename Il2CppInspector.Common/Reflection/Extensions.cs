/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Reflection
{
    public static class Extensions
    {
        // Convert a list of CustomAttributeData objects into C#-friendly attribute usages
        public static string ToString(this IEnumerable<CustomAttributeData> attributes, Scope scope = null,
            string linePrefix = "", string attributePrefix = "", bool inline = false, bool emitPointer = false, bool mustCompile = false) {
            var sb = new StringBuilder();

            foreach (var cad in attributes) {
                if (cad.CtorInfo != null)
                {
                    // v29+ attribute handling
                    // We now have much more information, so we can reconstruct the actual attribute
                    var ctor = cad.CtorInfo;

                    var name = ctor.Ctor.DeclaringType.GetScopedCSharpName(scope);
                    var suffix = name.LastIndexOf("Attribute", StringComparison.Ordinal);
                    if (suffix != -1)
                        name = name[..suffix];

                    sb.Append(linePrefix);
                    sb.Append('[');
                    sb.Append(attributePrefix);
                    sb.Append(name);

                    var totalCount = ctor.Arguments.Length + ctor.Fields.Length + ctor.Properties.Length;

                    if (totalCount > 0)
                    {
                        // We have parameters, need to use brackets
                        sb.Append('(');

                        var totalIndex = 0;
                        foreach (var argument in ctor.Arguments)
                        {
                            sb.Append(argument.Value.ToCSharpValue(argument.Type, scope));
                            if (++totalIndex != totalCount)
                                sb.Append(", ");
                        }

                        foreach (var field in ctor.Fields)
                        {
                            sb.Append(field.Field.CSharpName);
                            sb.Append(" = ");
                            sb.Append(field.Value.ToCSharpValue(field.Type, scope));
                            if (++totalIndex != totalCount)
                                sb.Append(", ");
                        }

                        foreach (var property in ctor.Properties)
                        {
                            sb.Append(property.Property.CSharpName);
                            sb.Append(" = ");
                            sb.Append(property.Value.ToCSharpValue(property.Type, scope));
                            if (++totalIndex != totalCount)
                                sb.Append(", ");
                        }

                        sb.Append(')');
                    }

                    sb.Append(']');
                    sb.Append(inline ? " " : "\n");
                }
                else
                {
                    // Pre-v29 attribute handling

                    // Find a constructor that either has no parameters, or all optional parameters
                    var parameterlessConstructor = cad.AttributeType.DeclaredConstructors.Any(c => !c.IsStatic && c.IsPublic && c.DeclaredParameters.All(p => p.IsOptional));

                    // IL2CPP doesn't retain attribute arguments so we have to comment out those with non-optional arguments if we want the output to compile
                    var commentStart = mustCompile && !parameterlessConstructor ? inline ? "/* " : "// " : "";
                    var commentEnd = commentStart.Length > 0 && inline ? " */" : "";
                    var arguments = "";

                    // Set AttributeUsage(AttributeTargets.All) if making output that compiles to mitigate CS0592
                    if (mustCompile && cad.AttributeType.FullName == "System.AttributeUsageAttribute")
                    {
                        commentStart = "";
                        commentEnd = "";
                        arguments = "(AttributeTargets.All)";
                    }

                    var name = cad.AttributeType.GetScopedCSharpName(scope);
                    var suffix = name.LastIndexOf("Attribute", StringComparison.Ordinal);
                    if (suffix != -1)
                        name = name[..suffix];
                    sb.Append($"{linePrefix}{commentStart}[{attributePrefix}{name}{arguments}]{commentEnd}");
                    if (emitPointer)
                        sb.Append($" {(inline ? "/*" : "//")} {cad.VirtualAddress.ToAddressString()}{(inline ? " */" : "")}");
                    sb.Append(inline ? " " : "\n");
                }
            }

            return sb.ToString();
        }

        // Output a ulong as a 32 or 64-bit hexadecimal address
        public static string ToAddressString(this ulong address) => address <= 0xffff_ffff
            ? string.Format($"0x{(uint)address:X8}")
            : string.Format($"0x{address:X16}");

        public static string ToAddressString(this long address) => ((ulong) address).ToAddressString();

        public static string ToAddressString(this (ulong start, ulong end)? address) => ToAddressString(address?.start ?? 0) + "-" + ToAddressString(address?.end ?? 0);

        public static string ToAddressString(this (ulong start, ulong end) address) => ToAddressString(address.start) + "-" + ToAddressString(address.end);

        // C# string literal escape characters
        // Taken from: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#regular-and-verbatim-string-literals
        private static Dictionary<char, string> escapeChars = new Dictionary<char, string> {
            ['\''] = @"\'",
            ['"'] = @"\""",
            ['\\'] = @"\\",
            ['\0'] = @"\0",
            ['\a'] = @"\a",
            ['\b'] = @"\b",
            ['\f'] = @"\f",
            ['\n'] = @"\n",
            ['\r'] = @"\r",
            ['\t'] = @"\t",
            ['\v'] = @"\v"
        };

        // Output a string in Python-friendly syntax
        public static string ToEscapedString(this string str) => str.ToEscapedString(escapeNonAscii: true);

        private static string ToEscapedString(this string str, bool escapeNonAscii) {
            // Replace standard escape characters
            var s = new StringBuilder();

            for (var i = 0; i < str.Length; i++)
            {
                var chr = str[i];
                if (escapeChars.TryGetValue(chr, out var escaped))
                    s.Append(escaped);
                else if ((escapeNonAscii && chr > 126) || CharUnicodeInfo.GetUnicodeCategory(str, i) is
                    UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or
                    UnicodeCategory.ParagraphSeparator or UnicodeCategory.Surrogate)
                {
                    s.Append("\\u");
                    s.Append($"{(int) chr:X4}");
                }
                else if (char.IsSurrogatePair(str, i))
                {
                    // Keep printable supplementary characters together; lone surrogates are escaped above.
                    s.Append(str, i, 2);
                    i++;
                }
                else
                    s.Append(chr);
            }

            return s.ToString();
        }

        public static string ToCIdentifier(this string str, bool allowScopeQualifiers = false) {
            // replace * with Ptr
            str = str.Replace("*", "Ptr");
            // escape non-ASCII characters
            var s = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
                if (str[i] < 32 || str[i] > 126)
                    s.Append($"u{(int) str[i]:X4}");
                else
                    s.Append(str[i]);
            str = s.ToString();
            // replace illegal characters
            str = Regex.Replace(str, allowScopeQualifiers? @"[^a-zA-Z0-9_\.:]" : "[^a-zA-Z0-9_]", "_");
            // ensure identifier starts with a letter or _ (and is non-empty)
            if (!Regex.IsMatch(str, "^[a-zA-Z_]"))
                str = "_" + str;
            return str;
        }

        // Output a value in C#-friendly syntax
        public static string ToCSharpValue(this object value, TypeInfo type, Scope usingScope = null) {
            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";
                case float f:
                    return value switch {
                        float.PositiveInfinity => "1F / 0F",
                        float.NegativeInfinity => "-1F / 0F",
                        float.NaN => "0F / 0F",
                        _ => f.ToString(CultureInfo.InvariantCulture) + "f"
                    };
                case double d:
                    return value switch {
                        double.PositiveInfinity => "1D / 0D",
                        double.NegativeInfinity => "-1D / 0D",
                        double.NaN => "0D / 0D",
                        _ => d.ToString(CultureInfo.InvariantCulture)
                    };
                case string str:
                    return $"\"{str.ToEscapedString(escapeNonAscii: false)}\"";
                case char c:
                    return $"'{c.ToString().ToEscapedString(escapeNonAscii: false)}'";
                case TypeInfo typeInfo:
                    return $"typeof({typeInfo.GetScopedCSharpName(usingScope)})";
                case CustomAttributeArgument[] array:
                    var arraySb = new StringBuilder();
                    arraySb.Append("new ");
                    arraySb.Append(type.GetScopedCSharpName(usingScope));
                    arraySb.Append('[');
                    arraySb.Append(array.Length);
                    arraySb.Append(']');

                    if (array.Length > 0)
                    {
                        arraySb.Append(" {");
                        for (int i = 0; i < array.Length; i++)
                        {
                            arraySb.Append(array[i].Value.ToCSharpValue(array[i].Type, usingScope));

                            if (i + 1 != array.Length)
                                arraySb.Append(", ");
                        }
                        arraySb.Append(" }");
                    }

                    return arraySb.ToString();
            }

            if (type.IsEnum) {
                var flags = type.GetCustomAttributes("System.FlagsAttribute").Any();
                var values = type.GetEnumNames().Zip(type.GetEnumValues().OfType<object>(), (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
                var typeName = type.GetScopedCSharpName(usingScope);

                // We don't know what type the enumeration or value is, so we use Object.Equals() to do content-based equality testing
                if (!flags) {
                    // Defined enum name
                    if (values.FirstOrDefault(v => v.Value.Equals(value)).Key is string enumValue)
                        return typeName + "." + enumValue;

                    // Undefined enum value (return a cast)
                    return "(" + typeName + ") " + value;
                }

                // Logical OR a series of flags together

                // Values like 0x8000_0000_0000_0000 can't be cast to Int64
                // but values like 0xffff_ffff can't be cast to UInt64 (due to sign extension)
                // so we're just going to have to try to find a type that doesn't make it explode
                if (value is byte || value is ushort || value is uint || value is ulong) {
                    var flagValue = Convert.ToUInt64(value);
                    var setFlags = values.Where(x => (Convert.ToUInt64(x.Value) & flagValue) == Convert.ToUInt64(x.Value)).Select(x => typeName + "." + x.Key);
                    return string.Join(" | ", setFlags);
                }
                else if (value is sbyte || value is short || value is int || value is long) {
                    var flagValue = Convert.ToInt64(value);
                    var setFlags = values.Where(x => (Convert.ToInt64(x.Value) & flagValue) == Convert.ToInt64(x.Value)).Select(x => typeName + "." + x.Key);
                    return string.Join(" | ", setFlags);
                } else {
                    throw new ArgumentException("Unsupported enum underlying type");
                }
            }
            // Structs and generic type parameters must use 'default' rather than 'null'
            return value?.ToString() ?? (type.IsValueType || type.IsGenericParameter? "default" : "null");
        }
    }
}
