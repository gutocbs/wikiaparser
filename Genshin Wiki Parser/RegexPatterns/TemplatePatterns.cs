using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class TemplatePatterns
{
    [GeneratedRegex(@"\{\{[^{}|]+\|([^{}]+)\}\}")]
    public static partial Regex TemplateWithPipe();

    [GeneratedRegex(@"\{\{([^{}|]+)\}\}")]
    public static partial Regex TemplateWithoutPipe();

    [GeneratedRegex(@"\|\s*(?<k>[\w\-]+)\s*=\s*(?<v>.*?)(?=\n\||\n\}\}|\r\n\||\r\n\}\}|$)", RegexOptions.Singleline)]
    public static partial Regex TemplateParam();
}