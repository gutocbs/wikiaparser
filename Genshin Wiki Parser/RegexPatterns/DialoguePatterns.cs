using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class DialoguePatterns
{
    [GeneratedRegex(@"\{\{\s*Quote\s*\|\s*(.*?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex QuoteTemplate();

    [GeneratedRegex(@"\{\{\s*Dialogue\s+Start\s*\}\}(?<body>[\s\S]*?)\{\{\s*Dialogue\s+End\s*\}\}", RegexOptions.IgnoreCase)]
    public static partial Regex DialogueBlock();

    [GeneratedRegex(@"\{\{\s*Dialogue Start\s*\}\}", RegexOptions.IgnoreCase)]
    public static partial Regex DialogueStart();

    [GeneratedRegex(@"\{\{\s*Dialogue End\s*\}\}", RegexOptions.IgnoreCase)]
    public static partial Regex DialogueEnd();

    [GeneratedRegex(@"\{\{\s*A\s*\|\s*([^}|]+)[^}]*\}\}", RegexOptions.IgnoreCase)]
    public static partial Regex AudioTemplate();

    [GeneratedRegex(@"\{\{\s*DIcon(?:\|[^}]*)?\}\}\s*", RegexOptions.IgnoreCase)]
    public static partial Regex DialogueIcon();

    [GeneratedRegex(@"^\{\{\s*DIcon", RegexOptions.IgnoreCase)]
    public static partial Regex DialogueIconStart();
}