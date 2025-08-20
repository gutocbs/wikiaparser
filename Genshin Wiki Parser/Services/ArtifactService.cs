using Genshin.Wiki.Parser.Models.Artifacts;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Artifact;

namespace Genshin.Wiki.Parser.Services;

public class ArtifactService
{
    public bool Set(Page page, string wikiText, string key)
    {
        ArtifactPieceDto? artifactPiece = ArtifactParser.TryParse(wikiText);
        if (artifactPiece is not null)
        {
            page.About = artifactPiece;
            return true;
        }
        return false;
    }
}