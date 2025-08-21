using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Faction;

public sealed class FactionDto : BaseDto
{
    public FactionDto()
    {
        ObjectType = ObjectTypeEnum.Faction;
    }
    
    // Infobox
    public string? Base { get; set; }
    public string? Region { get; set; }

    // Conteúdo textual
    public List<FactionQuoteDto>? Quotes { get; set; }
    public string? History { get; set; }
    public string? Responsibilities { get; set; }
    public string? Associates { get; set; }

    // Estrutura
    public List<FactionMemberDto>? Members { get; set; }
    public List<FactionFormerMemberDto>? FormerMembers { get; set; }
    public Dictionary<string, List<FactionEmployeeDto>>? EmployeesByGroup { get; set; } // ex.: "Wangshu Inn" -> lista
}