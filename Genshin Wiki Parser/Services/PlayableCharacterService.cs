using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Character;

namespace Genshin.Wiki.Parser.Services;

public class PlayableCharacterService
{
    private readonly Dictionary<string, PlayableCharacterDto> _charsByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LoreDto> _loreByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VoiceOversDto> _voByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CompanionDto> _companionByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NamecardDto> _namecardByKey = new(StringComparer.OrdinalIgnoreCase);

    private static bool ContainsCharacterTabs(string wikitext)
        => wikitext.IndexOf("CharacterTabs", StringComparison.OrdinalIgnoreCase) >= 0;
    
    public bool Set(Page page, string wikiText, string key)
    {
        if (!ContainsCharacterTabs(wikiText))
            return false;
        
        PlayableCharacterDto? characterDto = CharacterParser.TryParse(wikiText);
        if (characterDto != null)
        {
            page.About = characterDto;

            // liga um Lore pendente (se já tivermos visto /Lore antes)
            if (_loreByKey.TryGetValue(key, out LoreDto? loreWaiting))
            {
                characterDto.Lore = loreWaiting;
                _loreByKey.Remove(key);
            }
            
            if (_voByKey.TryGetValue(key, out VoiceOversDto? voiceOverWaiting))
            {
                characterDto.VoiceOvers = voiceOverWaiting;
                _loreByKey.Remove(key);
            }
            
            if (_companionByKey.TryGetValue(key, out CompanionDto? companionWaiting))
            {
                characterDto.Companion = companionWaiting;
                _companionByKey.Remove(key);
            }
            
            if (_namecardByKey.TryGetValue(key, out NamecardDto? namecardWaiting))
            {
                characterDto.Namecard = namecardWaiting;
                _namecardByKey.Remove(key);
            }

            // indexa/atualiza o character
            _charsByKey[key] = characterDto;

            return true;
        }
        
        if(page.title.Contains("/Lore", StringComparison.OrdinalIgnoreCase))
        {
            // 2) Tenta parsear Lore (só faz sentido em páginas X/Lore)
            LoreDto? lore = LoreParser.TryParse(wikiText, page.title);
            if (lore != null)
            {
                if (_charsByKey.TryGetValue(key, out PlayableCharacterDto? parentChar))
                    parentChar.Lore = lore; // já temos o personagem
                else
                    _loreByKey[key] = lore; // guardamos até achar o personagem base

                return true;
            }
        }
        
        if(page.title.Contains("/Voice-Overs", StringComparison.OrdinalIgnoreCase))
        {
            // 2) Tenta parsear
            
            VoiceOversDto? vo = VoiceOverParser.TryParse(wikiText, page.title);
            if (vo != null)
            {
                if (_charsByKey.TryGetValue(key, out PlayableCharacterDto? parentChar))
                    parentChar.VoiceOvers = vo; // já temos o personagem
                else
                    _voByKey[key] = vo; // guardamos até achar o personagem base

                return true;
            }
        }
        
        if(page.title.Contains("/Companion", StringComparison.OrdinalIgnoreCase))
        {
            CompanionDto? companion = CompanionParser.TryParse(wikiText, page.title);
            if (companion != null)
            {
                if (_charsByKey.TryGetValue(key, out PlayableCharacterDto? parentChar))
                    parentChar.Companion = companion; // já temos o personagem
                else
                    _companionByKey[key] = companion; // guardamos até achar o personagem base

                return true;
            }
        }

        //Namecard, tenta parsear
        if(page.title.Contains(":", StringComparison.OrdinalIgnoreCase) && wikiText.Contains("is a [[Namecard]] obtained by", StringComparison.OrdinalIgnoreCase))
        {
            NamecardDto? namecard = NamecardParser.TryParse(wikiText, page.title);
            if (namecard != null)
            {
                if (_charsByKey.TryGetValue(page.title.Split(":").FirstOrDefault() ?? key, out PlayableCharacterDto? parentChar))
                    parentChar.Namecard = namecard; // já temos o personagem
                else
                    _namecardByKey[key] = namecard; // guardamos até achar o personagem base

                return true;
            }
        }

        return false;
    }
}