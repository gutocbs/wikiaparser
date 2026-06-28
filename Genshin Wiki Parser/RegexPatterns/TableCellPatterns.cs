using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class TableCellPatterns
{
    [GeneratedRegex(@"\[\[\s*File:([^|\]]+)", RegexOptions.IgnoreCase)]
    public static partial Regex FileCell();

    [GeneratedRegex(@"<\s*br\s*/?>", RegexOptions.IgnoreCase)]
    public static partial Regex CellBreak();

    [GeneratedRegex(@"\[\[([^[\]|]+)\|([^[\]]+)\]\]")]
    public static partial Regex CellWikiPipeLink();

    [GeneratedRegex(@"\[\[([^[\]]+)\]\]")]
    public static partial Regex CellWikiLink();
}