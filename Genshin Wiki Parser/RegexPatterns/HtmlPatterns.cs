using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class HtmlPatterns
{
    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    public static partial Regex HtmlComment();

    [GeneratedRegex(@"<ref[^>]*>.*?</ref>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex RefTag();

    [GeneratedRegex(@"</?p\s*?>", RegexOptions.IgnoreCase)]
    public static partial Regex ParagraphTag();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    public static partial Regex BreakTag();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline)]
    public static partial Regex HtmlTag();

    [GeneratedRegex(@"<ref[^>]*>(.*?)</ref>", RegexOptions.Singleline)]
    public static partial Regex RefTagCapture();

    [GeneratedRegex(@"<ref[^>/]*/>|<ref[^>]*>.*?</ref>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex RefTagOrSelfClosing();
    
    [GeneratedRegex(@"<\s*small[^>]*>(.*?)</\s*small\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex SmallTag();

    [GeneratedRegex(@"<\/?nowiki>", RegexOptions.IgnoreCase)]
    public static partial Regex NoWikiTag();
    
    [GeneratedRegex(@"<gallery[^>]*>(.*?)</gallery>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    public static partial Regex GalleryRegex();
}