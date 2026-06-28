using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.RegexPatterns;

internal static partial class DynamicPatterns
{
    private static readonly ConcurrentDictionary<string, Regex> TemplateStartRegexCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Regex> TemplateHeaderRegexCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Regex> SectionHeadingRegexCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Regex> TemplateRegexCache = new(StringComparer.OrdinalIgnoreCase);

    public static Regex GetTemplateStartRegex(string templateName)
        => TemplateStartRegexCache.GetOrAdd(templateName, static name =>
            new Regex(@"\{\{\s*" + Regex.Escape(name) + @"\b", RegexOptions.IgnoreCase | RegexOptions.Compiled));

    public static Regex GetTemplateHeaderRegex(string headerName)
        => TemplateHeaderRegexCache.GetOrAdd(headerName, static name =>
            new Regex(@"^\s*" + Regex.Escape(name) + @"\b", RegexOptions.IgnoreCase | RegexOptions.Compiled));

    public static Regex GetSectionHeadingRegex(string heading)
        => SectionHeadingRegexCache.GetOrAdd(heading, static name =>
            new Regex(@"^=+\s*" + Regex.Escape(name) + @"\s*=+\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled));

    public static Regex GetTemplateRegex(string name)
        => TemplateRegexCache.GetOrAdd(name, static templateName =>
            new Regex(@"\{\{\s*" + Regex.Escape(templateName) + @"\b(?<body>[\s\S]*?)\}\}", RegexOptions.IgnoreCase | RegexOptions.Compiled));
}