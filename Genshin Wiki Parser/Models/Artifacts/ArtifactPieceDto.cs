using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Artifacts;

public sealed class ArtifactPieceDto : BaseDto
{
    public ArtifactPieceDto()
    {
        ObjectType = ObjectTypeEnum.Artifact;
    }
    
    public string? Set { get; set; }                   // Long Night's Oath
    public string? Piece { get; set; }                 // Flower of Life (como vem no wiki)
    public string? ShortDescription { get; set; }      // {{Description|...}}
    public string? LongDescription { get; set; }       // seção ==Description==
}