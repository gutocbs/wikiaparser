using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Quest;
using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.Parsers.Quest;

public static class QuestParser
{
    public static QuestDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains("{{Quest Infobox", StringComparison.OrdinalIgnoreCase)) return null;

        var dto = new QuestDto { Title = pageTitle };

        // 1) Infobox
        var infobox = TextHelper.ExtractTemplate("Quest Infobox", wikiText);
        var map  = TextHelper.ParseTemplateParams(infobox);

        dto.Id           = TextHelper.TryInt(TextHelper.Get(map, "id"));
        dto.Type         = TextHelper.Get(map, "type");
        dto.Chapter      = TextHelper.Get(map, "chapter");
        dto.ActNum       = TextHelper.TryInt(TextHelper.Get(map, "actNum"));
        dto.Act          = TextHelper.Get(map, "act");
        dto.Part         = TextHelper.TryInt(TextHelper.Get(map, "part"));
        dto.Character    = TextHelper.CleanText(TextHelper.Get(map, "character"));

        dto.StartLocation= TextHelper.CleanText(TextHelper.Get(map, "startLocation"));
        dto.Region       = TextHelper.CleanText(TextHelper.Get(map, "region"));
        dto.Area         = TextHelper.CleanText(TextHelper.Get(map, "area"));
        dto.Subarea      = TextHelper.CleanText(TextHelper.Get(map, "subarea")).Replace(" (Subarea)", "");
        
        // elenco (separado por ';')
        var chars = TextHelper.Get(map, "characters");
        if (!string.IsNullOrWhiteSpace(chars))
        {
            foreach (var c in chars.Split(';'))
            {
                var v = TextHelper.CleanText(c);
                if (!string.IsNullOrWhiteSpace(v))
                    dto.Characters.Add(v.Trim());
            }
        }

        // 3) Descrição da Quest (template)
        dto.Description = ExtractQuestDescription(wikiText);

        // 4) Diálogos
        dto.Dialogues = ExtractDialogues(wikiText);

        return dto;
    }

    
    // ---------- Partes específicas de Quest ----------
    private static string? ExtractQuestDescription(string text)
    {
        foreach (var t in TextHelper.ExtractTemplates("Quest Description", text))
        {
            var inner = TextHelper.CleanText(t);
            if (!string.IsNullOrWhiteSpace(inner)) return inner.Replace("|","");
        }
        return null;
    }

    private static List<DialogueSection> ExtractDialogues(string text)
    {
        var result = new List<DialogueSection>();

        // pega blocos entre {{Dialogue Start}} ... {{Dialogue End}}
        var rx = new Regex(@"\{\{\s*Dialogue\s+Start\s*\}\}(?<body>[\s\S]*?)\{\{\s*Dialogue\s+End\s*\}\}",
                           RegexOptions.IgnoreCase);
        foreach (Match blk in rx.Matches(text))
        {
            var body = blk.Groups["body"].Value;
            var lines = body.Split('\n');
            var section = new DialogueSection();

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----")) continue;

                // Contexto de cena: ;( ... )
                var mCtx = Regex.Match(line, @"^\s*;\s*\((?<c>.+?)\)\s*$");
                if (mCtx.Success)
                {
                    // inicia nova seção quando encontrar próximo contexto
                    if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                    {
                        result.Add(section);
                        section = new DialogueSection();
                    }
                    section.Context = TextHelper.CleanText(mCtx.Groups["c"].Value);
                    continue;
                }

                // Linhas de diálogo/choices começam com ":"
                if (Regex.IsMatch(line, @"^\s*:"))
                {
                    // Choice: :{{DIcon}} Text
                    if (Regex.IsMatch(line, @"^\s*:\s*\{\{\s*DIcon", RegexOptions.IgnoreCase))
                    {
                        var textClean = TextHelper.CleanText(line);
                        if (!string.IsNullOrWhiteSpace(textClean))
                            section.Lines.Add(new DialogueLine { Speaker = "[Choice]", Text = textClean.Replace("DIcon","").Replace(":;","") });
                        continue;
                    }
                    
                    // Remove templates de áudio para facilitar parse do speaker/texto
                    var noAudio = Regex.Replace(line, @"\{\{\s*A\s*\|[^}]+\}\}", "", RegexOptions.IgnoreCase);

                    // Formato típico: : '''Speaker:''' Text
                    var mTalk = Regex.Match(noAudio, @"'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*(?<tx>.+)$");
                    if (!mTalk.Success)
                        mTalk = Regex.Match(noAudio, @"'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*(?<tx>.*)$");

                    if (mTalk.Success)
                    {
                        var sp = TextHelper.CleanText(mTalk.Groups["sp"].Value);
                        var tx = TextHelper.CleanText(mTalk.Groups["tx"].Value);
                        if (!string.IsNullOrWhiteSpace(tx))
                            section.Lines.Add(new DialogueLine { Speaker = sp, Text = tx.Replace(":;","") });
                    }
                    else
                    {
                        // fallback: limpa wiki e joga como narrativa sem speaker
                        var txt = TextHelper.CleanText(noAudio);
                        if (!string.IsNullOrWhiteSpace(txt))
                            section.Lines.Add(new DialogueLine { Speaker = null, Text = txt.Replace(":;","") });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                result.Add(section);
        }

        return result;
    }
}
