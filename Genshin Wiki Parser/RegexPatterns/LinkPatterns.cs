using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class LinkPatterns
{
    [GeneratedRegex(@"\[(https?://[^\s\]]+)\s+([^\]]+)\]")]
    public static partial Regex ExternalLink();

    [GeneratedRegex(@"\[\[([^\|\]]+)\|([^\]]+)\]\]")]
    public static partial Regex WikiPipeLink();

    [GeneratedRegex(@"\[\[([^\]]+)\]\]")]
    public static partial Regex WikiLink();

    [GeneratedRegex(@"\[\:\s*Category\:([^\]]+)\]", RegexOptions.IgnoreCase)]
    public static partial Regex CategoryLink();

    [GeneratedRegex(@"https?://[^\s\]]+")]
    public static partial Regex Url();
}