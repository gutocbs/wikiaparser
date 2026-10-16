using System.Xml;
using Genshin.Wiki.Parser.Models.XML;

namespace Genshin.Wiki.Parser.Services;

/// <summary>
/// Lê o dump XML do MediaWiki em streaming, uma página por vez, direto sobre um único XmlReader
/// (sem ReadSubtree por página/revisão, que adicionava uma camada em cada leitura).
/// </summary>
public static class MediaWikiPageReader
{
    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true
    };

    /// <summary>Enumera as páginas do dump na ordem do arquivo.</summary>
    /// <param name="inputPath">Caminho do dump XML.</param>
    /// <param name="shouldSkipTitle">Filtro opcional: páginas cujo título retorna true são puladas sem ler a revisão.</param>
    /// <returns>Páginas com título não vazio e o texto da última revisão ("" quando não há texto).</returns>
    public static IEnumerable<Page> ReadPages(string inputPath, Func<string, bool>? shouldSkipTitle = null)
    {
        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 128);
        using var reader = XmlReader.Create(stream, Settings);

        while (reader.ReadToFollowing("page"))
        {
            Page? page = ReadPage(reader, shouldSkipTitle);
            if (page is not null) yield return page;
        }
    }

    // Espera o reader em <page>; termina no </page> correspondente.
    private static Page? ReadPage(XmlReader reader, Func<string, bool>? shouldSkipTitle)
    {
        var page = new Page
        {
            title = string.Empty,
            id = null,
            revision = new Revision { text = new Text { content = string.Empty } }
        };

        if (reader.IsEmptyElement) return null;

        int pageDepth = reader.Depth;
        reader.Read();

        // ReadElementContentAsString/Skip já avançam o reader; por isso esses casos usam "continue" sem Read().
        while (!reader.EOF && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == pageDepth))
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != pageDepth + 1)
            {
                reader.Read();
                continue;
            }

            switch (reader.LocalName)
            {
                case "title":
                    page.title = reader.ReadElementContentAsString();
                    if (shouldSkipTitle?.Invoke(page.title) == true)
                    {
                        SkipToEndOfElement(reader, pageDepth);
                        return null;
                    }
                    continue;
                case "id" when page.id is null:
                    page.id = reader.ReadElementContentAsString();
                    continue;
                case "revision":
                    page.revision = ReadRevision(reader);
                    continue;
                default:
                    reader.Skip();
                    continue;
            }
        }

        return string.IsNullOrWhiteSpace(page.title) ? null : page;
    }

    // Espera o reader em <revision>; lê o primeiro <text> e pula o resto da revisão.
    private static Revision ReadRevision(XmlReader reader)
    {
        var revision = new Revision { text = new Text { content = string.Empty } };
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return revision;
        }

        int revisionDepth = reader.Depth;
        bool hasText = false;
        reader.Read();

        while (!reader.EOF && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == revisionDepth))
        {
            if (!hasText && reader.NodeType == XmlNodeType.Element && reader.LocalName == "text")
            {
                revision.text.content = reader.ReadElementContentAsString();
                hasText = true;
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element) reader.Skip();
            else reader.Read();
        }

        reader.Read(); // sai do </revision>
        return revision;
    }

    // Avança até o fechamento do elemento de profundidade "depth", pulando subárvores inteiras.
    private static void SkipToEndOfElement(XmlReader reader, int depth)
    {
        while (!reader.EOF && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
        {
            if (reader.NodeType == XmlNodeType.Element) reader.Skip();
            else reader.Read();
        }
    }
}
