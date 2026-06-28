using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class DescriptionPatterns
{
    [GeneratedRegex(@"\{\{\s*Description\s*\|\s*(.+?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex DescriptionTemplate();

    [GeneratedRegex(@"^\s*Description\s*\|\s*(.+)$", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex DescriptionTemplateBody();

    [GeneratedRegex(@"^==\s*Description\s*==\s*(.+?)(?=^\s*==|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    public static partial Regex DescriptionSection();
}