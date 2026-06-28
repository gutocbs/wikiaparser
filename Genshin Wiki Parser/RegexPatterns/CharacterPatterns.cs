using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class CharacterPatterns
{
    [GeneratedRegex(@"^\s*Character\s+Infobox\b", RegexOptions.IgnoreCase)]
    public static partial Regex CharacterInfoboxHeader();

    [GeneratedRegex(@"\b(is a playable|can be obtained)\b", RegexOptions.IgnoreCase)]
    public static partial Regex PlayableOrObtained();

    [GeneratedRegex(@"\b(is|was)\b", RegexOptions.IgnoreCase)]
    public static partial Regex IsOrWas();

    [GeneratedRegex(@"\{\{\s*Change\s+History\s*\|\s*([^}|]+)\s*\}\}", RegexOptions.IgnoreCase)]
    public static partial Regex ChangeHistoryVersion();
}