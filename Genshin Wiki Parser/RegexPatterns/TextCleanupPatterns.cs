using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class TextCleanupPatterns
{
    [GeneratedRegex(@"'{2,5}")]
    public static partial Regex WikiQuoteMarkup();

    [GeneratedRegex(@"''+")]
    public static partial Regex WikiApostropheMarkup();

    [GeneratedRegex(@"[ \t]+\n")]
    public static partial Regex TrailingHorizontalWhitespace();
    
    [GeneratedRegex(@"\n{3,}")]
    public static partial Regex MultipleNewLines();

    [GeneratedRegex(@"\s+")]
    public static partial Regex Whitespace();

    [GeneratedRegex(@"^\s*\*\s*", RegexOptions.Multiline)]
    public static partial Regex LeadingBullet();

    [GeneratedRegex(@"^'{2,}[^']+'{2,}$")]
    public static partial Regex QuotedOnly();

    [GeneratedRegex(@"^=+\s*.+?\s*=+\s*$", RegexOptions.Multiline)]
    public static partial Regex AnyHeading();
}