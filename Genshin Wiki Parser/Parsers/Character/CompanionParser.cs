using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class CompanionParser
{
    public static CompanionDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext) || string.IsNullOrWhiteSpace(pageTitle))
            return null;
        if (!pageTitle.EndsWith("/Companion", StringComparison.OrdinalIgnoreCase))
            return null;

        var character = TextHelper.BaseCharacterFromTitle(pageTitle);

        var dto = new CompanionDto { Character = character };

        // 2) Idle Quotes
        dto.IdleQuotes = ExtractIdleQuotes(wikitext);

        // 3) Dialogue principal
        var dialogues = new List<CompanionDialogueScenarioDto>();
        var mainDlg = ExtractDialogueScenario(wikitext, "Dialogue", scenarioTitle: null);
        if (mainDlg != null) dialogues.Add(mainDlg);

        // 4) Special Dialogue (subseções ===Title===)
        var specials = ExtractSpecialDialogues(wikitext);
        if (specials != null && specials.Count > 0) dialogues.AddRange(specials);

        dto.Dialogues = dialogues.Count > 0 ? dialogues : null;

        // nada encontrado?
        if (dto.IdleQuotes == null && (dto.Dialogues == null || dto.Dialogues.Count == 0))
            return null;

        return dto;
    }

    // ---------- Idle Quotes ----------
    private static List<CompanionQuoteDto>? ExtractIdleQuotes(string text)
    {
        var section = TextHelper.ExtractSection(text, "Idle Quotes");
        if (section == null) return null;

        var block = ExtractDialogueBlock(section);
        if (block == null) return null;

        var quotes = new List<CompanionQuoteDto>();
        string? currentContext = null;

        using var sr = new StringReader(block);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            // context lines iniciadas por ';' (ex.: ;(When the player is nearby))
            if (trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                var ctx = Regex.Replace(trimmed[1..].Trim(), @"^<nowiki>|</nowiki>$", "", RegexOptions.IgnoreCase).Trim();
                ctx = TextHelper.StripParens(ctx);
                currentContext = TextHelper.CleanInline(ctx);
                continue;
            }

            // linhas de fala iniciadas por ':' e contendo {{DIcon|Idle}} ou {{DIcon}}
            if (trimmed.StartsWith(":", StringComparison.Ordinal))
            {
                var t = trimmed.TrimStart(':').Trim();

                // remove ícones e marcações
                t = Regex.Replace(t, @"\{\{\s*DIcon(?:\|[^}]*)?\}\}\s*", "", RegexOptions.IgnoreCase);
                t = TextHelper.CleanText(t);

                if (!string.IsNullOrWhiteSpace(t))
                    quotes.Add(new CompanionQuoteDto { Text = t, Context = currentContext });
            }
        }

        return quotes.Count > 0 ? quotes : null;
    }

    // ---------- Dialogue (principal ou cenários especiais) ----------
    private static CompanionDialogueScenarioDto? ExtractDialogueScenario(string fullText, string sectionTitle, string? scenarioTitle)
    {
        var section = TextHelper.ExtractSection(fullText, sectionTitle);
        if (section == null) return null;

        // dentro da seção, pega entre {{Dialogue Start}} e {{Dialogue End}}
        var block = ExtractDialogueBlock(section);
        if (block == null) return null;

        var (entries, conditions) = ParseDialogueEntries(block);

        if (entries.Count == 0 && conditions.Count == 0) return null;

        return new CompanionDialogueScenarioDto
        {
            ScenarioTitle = scenarioTitle,
            Conditions = conditions.Count > 0 ? conditions : null,
            Entries = entries
        };
    }

    private static List<CompanionDialogueScenarioDto>? ExtractSpecialDialogues(string fullText)
    {
        var special = TextHelper.ExtractSection(fullText, "Special Dialogue");
        if (special == null) return null;

        var result = new List<CompanionDialogueScenarioDto>();

        // dividir por subheadings "=== Title ==="
        var matches = Regex.Matches(special, @"^===\s*(.+?)\s*===\s*$", RegexOptions.Multiline);
        if (matches.Count == 0)
        {
            // às vezes não há subtítulos; tenta parsear bloco diretamente
            var single = ExtractDialogueBlock(special);
            if (single != null)
            {
                var (entries, conditions) = ParseDialogueEntries(single);
                if (entries.Count > 0 || conditions.Count > 0)
                    result.Add(new CompanionDialogueScenarioDto { ScenarioTitle = null, Conditions = conditions.Count > 0 ? conditions : null, Entries = entries });
            }
            return result.Count > 0 ? result : null;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var title = TextHelper.CleanInline(matches[i].Groups[1].Value);
            int start = matches[i].Index + matches[i].Length;
            int end = (i + 1 < matches.Count) ? matches[i + 1].Index : special.Length;
            var chunk = special.Substring(start, end - start);

            var block = ExtractDialogueBlock(chunk);
            if (block == null) continue;

            var (entries, conditions) = ParseDialogueEntries(block);
            if (entries.Count == 0 && conditions.Count == 0) continue;

            result.Add(new CompanionDialogueScenarioDto
            {
                ScenarioTitle = title,
                Conditions = conditions.Count > 0 ? conditions : null,
                Entries = entries
            });
        }

        return result.Count > 0 ? result : null;
    }

    private static (List<CompanionDialogueEntryDto> entries, List<string> conditions) ParseDialogueEntries(string dialogueBlock)
    {
        var entries = new List<CompanionDialogueEntryDto>();
        var conditions = new List<string?>();
        string? currentChoiceGroup = null;

        using var sr = new StringReader(dialogueBlock);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            var raw = line.Trim();

            if (string.IsNullOrWhiteSpace(raw)) continue;

            // linhas de condição começando com ';' (inclui <nowiki>...<nowiki>)
            if (raw.StartsWith(";", StringComparison.Ordinal))
            {
                var cond = Regex.Replace(raw[1..].Trim(), @"^<nowiki>|</nowiki>$", "", RegexOptions.IgnoreCase).Trim();
                cond = TextHelper.StripParens(cond);
                cond = TextHelper.CleanInline(cond);
                if (!string.IsNullOrWhiteSpace(cond)) conditions.Add(cond);
                continue;
            }

            // linhas de “player choice”: ":{{DIcon}} Pergunta"
            if (raw.StartsWith(":", StringComparison.Ordinal))
            {
                var body = raw.TrimStart(':').Trim();

                if (Regex.IsMatch(body, @"\{\{\s*DIcon(?:\|[^}]*)?\}\}", RegexOptions.IgnoreCase))
                {
                    // inicia/continua um grupo de escolhas
                    currentChoiceGroup ??= Guid.NewGuid().ToString("N");

                    // remove {{DIcon}} e limpa
                    body = Regex.Replace(body, @"\{\{\s*DIcon(?:\|[^}]*)?\}\}\s*", "", RegexOptions.IgnoreCase);
                    body = TextHelper.CleanText(body);

                    if (!string.IsNullOrWhiteSpace(body))
                        entries.Add(new CompanionDialogueEntryDto { Role = "Player", Text = body, ChoiceGroup = currentChoiceGroup });

                    continue;
                }

                // fala do NPC na forma de um único ':' com áudio embutido
                // padrão: {{A|file.ogg}} '''Nome:''' Texto
                var audio = ExtractAudioFiles(body, out var remainder);
                var spoken = TextHelper.CleanText(RemoveSpeakerBold(remainder));

                if (!string.IsNullOrWhiteSpace(spoken) || (audio?.Count ?? 0) > 0)
                    entries.Add(new CompanionDialogueEntryDto { Role = "NPC", Text = spoken, AudioFiles = audio, ChoiceGroup = currentChoiceGroup });

                continue;
            }

            // linhas de resposta do NPC vinculadas a uma escolha começam com '::'
            if (raw.StartsWith("::", StringComparison.Ordinal))
            {
                var body = raw.TrimStart(':').TrimStart(':').Trim();
                var audio = ExtractAudioFiles(body, out var remainder);
                var spoken = TextHelper.CleanText(RemoveSpeakerBold(remainder));

                if (!string.IsNullOrWhiteSpace(spoken) || (audio?.Count ?? 0) > 0)
                    entries.Add(new CompanionDialogueEntryDto { Role = "NPC", Text = spoken, AudioFiles = audio, ChoiceGroup = currentChoiceGroup });

                continue;
            }

            // reset do grupo se aparecer uma linha fora do fluxo de escolhas
            if (!raw.StartsWith(":", StringComparison.Ordinal) && !raw.StartsWith("::", StringComparison.Ordinal))
                currentChoiceGroup = null;
        }

        return (entries, conditions);
    }

    private static List<string>? ExtractAudioFiles(string? text, out string? withoutAudio)
    {
        var files = new List<string?>();
        withoutAudio = text;

        var rx = new Regex(@"\{\{\s*A\s*\|\s*([^}|]+)\s*\}\}", RegexOptions.IgnoreCase);
        withoutAudio = rx.Replace(withoutAudio, m =>
        {
            var f = TextHelper.CleanInline(m.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(f)) files.Add(f);
            return ""; // remove tag de áudio do texto
        });

        if (files.Count == 0) return null;
        return files;
    }

    private static string RemoveSpeakerBold(string? text)
    {
        // remove padrões como: '''Faruzan:''' no começo da fala
        var s = text.Trim();
        s = Regex.Replace(s, @"^'{2,5}[^']+:'{2,5}\s*", "", RegexOptions.IgnoreCase);
        return s;
    }

    private static string? ExtractDialogueBlock(string section)
    {
        var start = Regex.Match(section, @"\{\{\s*Dialogue Start\s*\}\}", RegexOptions.IgnoreCase);
        var end   = Regex.Match(section, @"\{\{\s*Dialogue End\s*\}\}",   RegexOptions.IgnoreCase);
        if (!start.Success || !end.Success || end.Index <= start.Index) return null;
        return section.Substring(start.Index + start.Length, end.Index - (start.Index + start.Length)).Trim();
    }
}
