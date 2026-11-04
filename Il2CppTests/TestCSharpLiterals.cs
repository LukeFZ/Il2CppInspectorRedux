using System;
using System.IO;
using System.Linq;
using System.Text;
using Il2CppInspector.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public class TestCSharpLiterals
    {
        [Test]
        public void GeneratedLiteralsPreserveUnicodeAndCompileWithoutChangingValues() {
            var readable = new[] { "这是中文", "café Ελληνικά Кириллица العربية", "日本語 한국어", "😀 𠀀" };
            foreach (var value in readable)
                Assert.That(value.ToCSharpValue(null), Is.EqualTo($"\"{value}\""));
            Assert.That('中'.ToCSharpValue(null), Is.EqualTo("'中'"));

            var strings = readable.Concat(new[] {
                "", "ASCII", "\"'\\\\u4E2D",
                "\0\a\b\f\n\r\t\v\u001F\u007F\u0085",
                "\u2028\u2029\u200B\u202E\uFEFF",
                "\uD800", "\uDC00", "\uDC00\uD800", "\uD800x\uDC00", "\uD800\uD800\uDC00",
                "\U000E0001",
                new string(Enumerable.Range(0, char.MaxValue + 1).Select(c => (char) c).ToArray())
            }).ToArray();
            var characters = new[] { '中', 'é', '\'', '"', '\\', '\0', '\a', '\b', '\f', '\n', '\r', '\t', '\v',
                '\u001F', '\u007F', '\u0085', '\u2028', '\u2029', '\u200B', '\u202E', '\uFEFF', '\uD800', '\uDC00' };

            // Exercise the formatter used by fields, optional parameters and attribute arguments.
            var source = new StringBuilder("public class LiteralAttribute : System.Attribute { public LiteralAttribute(string value) {} }\npublic class Literals {\n");
            for (var i = 0; i < strings.Length; i++) {
                var literal = strings[i].ToCSharpValue(null);
                // Attribute blobs use UTF-8, so only put well-formed Unicode in attribute arguments.
                if (i < readable.Length)
                    source.AppendLine($"[Literal({literal})]");
                source.AppendLine($"public const string String{i} = {literal};");
                source.AppendLine($"public static void StringDefault{i}(string value = {literal}) {{}}");
            }
            for (var i = 0; i < characters.Length; i++) {
                var literal = characters[i].ToCSharpValue(null);
                source.AppendLine($"public const char Char{i} = {literal};");
                source.AppendLine($"public static void CharDefault{i}(char value = {literal}) {{}}");
            }
            source.AppendLine("}");

            // Match the UTF-8 source output and reject raw, unpaired surrogates.
            var utf8 = new UTF8Encoding(false, true);
            using var sourceStream = new MemoryStream(utf8.GetBytes(source.ToString()));
            var syntaxTree = CSharpSyntaxTree.ParseText(Microsoft.CodeAnalysis.Text.SourceText.From(sourceStream, utf8));
            var compilation = CSharpCompilation.Create("Literals_" + Guid.NewGuid().ToString("N"),
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var assemblyStream = new MemoryStream();
            var result = compilation.Emit(assemblyStream);
            Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));

            var type = System.Reflection.Assembly.Load(assemblyStream.ToArray()).GetType("Literals");
            for (var i = 0; i < strings.Length; i++) {
                var field = type.GetField($"String{i}");
                Assert.That(field.GetRawConstantValue(), Is.EqualTo(strings[i]), $"String constant {i}");
                Assert.That(type.GetMethod($"StringDefault{i}").GetParameters()[0].DefaultValue,
                    Is.EqualTo(strings[i]), $"String default {i}");
                if (i < readable.Length)
                    Assert.That(field.GetCustomAttributesData().Single().ConstructorArguments[0].Value,
                        Is.EqualTo(strings[i]), $"Attribute argument {i}");
            }
            for (var i = 0; i < characters.Length; i++) {
                Assert.That(type.GetField($"Char{i}").GetRawConstantValue(), Is.EqualTo(characters[i]), $"Character constant {i}");
                Assert.That(type.GetMethod($"CharDefault{i}").GetParameters()[0].DefaultValue,
                    Is.EqualTo(characters[i]), $"Character default {i}");
            }

            // Python and JSON callers must retain their existing ASCII-only escaping.
            Assert.That("这是中文😀".ToEscapedString(), Is.EqualTo(@"\u8FD9\u662F\u4E2D\u6587\uD83D\uDE00"));
            Assert.That("\"'\\\n\u007F".ToEscapedString(), Is.EqualTo("\\\"\\'\\\\\\n\\u007F"));
        }
    }
}
