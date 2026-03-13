using System.Text;

namespace VersionedSerialization.Generator.Utils;

public class CodeGenerator
{
    private const string Indent = "    ";

    private readonly StringBuilder _sb = new();
    private string _currentIndent = "";

    public void EnterScope(string? header = null)
    {
        if (header != null)
        {
            AppendLine(header);
        }

        AppendLine("{");
        IncreaseIndentation();
    }

    public void LeaveScope(string suffix = "")
    {
        DecreaseIndentation();

        _sb.Append(_currentIndent);
        _sb.Append('}');
        _sb.AppendLine(suffix);
    }

    public void IncreaseIndentation()
    {
        _currentIndent += Indent;
    }

    public void DecreaseIndentation()
    {
        _currentIndent = _currentIndent[..^Indent.Length];
    }

    public void AppendLine()
    {
        _sb.AppendLine();
    }

    public void AppendLine(string text)
    {
        _sb.Append(_currentIndent);
        _sb.AppendLine(text);
    }

    public override string ToString()
        => _sb.ToString();
}