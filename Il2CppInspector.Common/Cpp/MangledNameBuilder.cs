#nullable enable
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp;

// This follows Itanium/GCC mangling specifications.
public partial class MangledNameBuilder
{
    private readonly StringBuilder _sb = new("_Z");
    private readonly Dictionary<TypeInfo, int> _substitutionMap = [];
    private int _currentSubstitutionIndex;

    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex Gcc { get; }

    public override string ToString()
        => _sb.ToString();

    public static string Method(MethodBase method)
    {
        var builder = new MangledNameBuilder();
        builder.BuildMethod(method);
        return builder.ToString();
    }

    public static string MethodInfo(MethodBase method)
    {
        var builder = new MangledNameBuilder();
        builder.BuildMethod(method, "MethodInfo");
        return builder.ToString();
    }

    public static string TypeInfo(TypeInfo type)
    {
        var builder = new MangledNameBuilder();
        builder.BuildData(type, "TypeInfo");
        return builder.ToString();
    }

    public static string TypeRef(TypeInfo type)
    {
        var builder = new MangledNameBuilder();
        builder.BuildData(type, "TypeRef");
        return builder.ToString();
    }

    private static ReadOnlySpan<char> Base36Alphabet => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private bool TryWriteSubstitution(TypeInfo obj)
    {
        if (!_substitutionMap.TryGetValue(obj, out var index))
            return false;

        _sb.Append('S');

        if (index > 0)
        {
            index -= 1; // The first one uses 'S_', the next one 'S0_', etc.

            var buffer = (stackalloc char[8]);
            var offset = buffer.Length;

            while (index > 0)
            {
                buffer[--offset] = Base36Alphabet[index % 36];
                index /= 36;
            }

            _sb.Append(buffer[offset..]);
        }
        _sb.Append('_');

        return true;
    }

    private void RegisterSubstitution(TypeInfo obj)
    {
        _substitutionMap[obj] = _currentSubstitutionIndex++;
    }

    private void SkipSubstitutionIndex()
    {
        _currentSubstitutionIndex++;
    }

    private void BuildData(TypeInfo type, string prefix = "")
    {
        BeginName();

        if (prefix.Length > 0)
        {
            WriteIdentifier(prefix);
            SkipSubstitutionIndex();
        }

        WriteTypeName(type);
        RegisterSubstitution(type);

        WriteEnd();
    }

    private void BuildMethod(MethodBase method, string prefix = "")
    {
        /*
         * We do not have any CV-qualifiers nor ref-qualifiers,
         * so we immediately write the nested name.
         */

        BeginName();

        if (prefix.Length > 0)
        {
            WriteIdentifier(prefix);
            SkipSubstitutionIndex();
        }

        WriteTypeName(method.DeclaringType);
        RegisterSubstitution(method.DeclaringType);

        switch (method.Name)
        {
            case ".ctor":
                _sb.Append("C1"); // Constructor
                break;
            case ".cctor":
                WriteIdentifier("cctor");
                break;
            default:
                WriteIdentifier(method.Name);
                break;
        }

        var genericParams = method.GetGenericArguments();

        if (genericParams.Length > 0)
        {
            SkipSubstitutionIndex();
            WriteGenericParams(genericParams);
        }

        WriteEnd(); // End nested name

        // Now write the method parameters

        if (genericParams.Length > 0 && method is MethodInfo mInfo)
        {
            // If this is a generic method, the first parameter needs to be the return type
            WriteType(mInfo.ReturnType);
        }

        if (method.DeclaredParameters.Count == 0)
        {
            _sb.Append('v');
        }
        else
        {
            foreach (var param in method.DeclaredParameters)
                WriteType(param.ParameterType);
        }

        SkipSubstitutionIndex();
    }

    private void WriteTypeName(TypeInfo type)
    {
        if (type.HasElementType)
            type = type.ElementType;

        foreach (var part in type.Namespace.Split("."))
        {
            if (part.Length > 0)
            {
                WriteIdentifier(part);
                SkipSubstitutionIndex();
            }
        }

        if (type.DeclaringType != null)
        {
            WriteIdentifier(type.DeclaringType.Name);
            SkipSubstitutionIndex();
        }

        WriteIdentifier(type.CSharpBaseName);

        if (type.GenericTypeArguments.Length > 0)
        {
            SkipSubstitutionIndex();
            WriteGenericParams(type.GenericTypeArguments);
        }
    }

    private void WriteType(TypeInfo type)
    {
        if (type.FullName == "System.Void")
        {
            _sb.Append('v');
            return;
        }

        var isNestedTypeElement = false;

        if (type.IsByRef)
        {
            _sb.Append('R');
            isNestedTypeElement = true;
        }

        if (type.IsPointer)
        {
            _sb.Append('P');
            isNestedTypeElement = true;
        }

        if (type.IsArray)
        {
            _sb.Append("A_");
            isNestedTypeElement = true;
        }

        if (type.IsPrimitive && type.Name != "Decimal")
        {
            if (type.Name is "IntPtr" or "UIntPtr")
            {
                _sb.Append("Pv"); // void*
            }
            else
            {
                _sb.Append(type.Name switch
                {
                    "Boolean" => 'b',
                    "Byte" => 'h',
                    "SByte" => 'a',
                    "Int16" => 's',
                    "UInt16" => 't',
                    "Int32" => 'i',
                    "UInt32" => 'j',
                    "Int64" => 'l',
                    "UInt64" => 'm',
                    "Char" => 'w',
                    "Single" => 'f',
                    "Double" => 'd',
                    _ => throw new UnreachableException()
                });
            }
        }
        else
        {
            if (!TryWriteSubstitution(type))
            {
                BeginName();
                WriteTypeName(type);
                WriteEnd();

                RegisterSubstitution(type);
            }
        }

        if (isNestedTypeElement)
        {
            SkipSubstitutionIndex();
        }
    }

    private void WriteGenericParams(TypeInfo[] generics)
    {
        BeginGenerics();

        foreach (var arg in generics)
        {
            WriteType(arg);
        }

        WriteEnd();
    }

    private void WriteIdentifier(string identifier)
    {
        identifier = Gcc.Replace(identifier, "_");

        _sb.Append(identifier.Length);
        _sb.Append(identifier);
    }

    private void BeginName()
    {
        _sb.Append('N');
    }

    private void BeginGenerics()
    {
        _sb.Append('I');
    }

    private void WriteEnd()
    {
        _sb.Append('E');
    }
}