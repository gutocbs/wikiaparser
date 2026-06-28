using System.Xml;
using Genshin.Wiki.Parser.Models.XML;

namespace Genshin.Wiki.Parser.Services;

public static class MediaWikiPageReader
{
    public static IEnumerable<Page> ReadPages(string inputPath, Func<string, bool>? shouldSkipTitle = null)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };

        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 128);
        using var reader = XmlReader.Create(stream, settings);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "page") continue;

            using var pageReader = reader.ReadSubtree();
            Page? page = ReadPage(pageReader, shouldSkipTitle);
            if (page is not null) yield return page;
        }
    }

    private static Page? ReadPage(XmlReader reader, Func<string, bool>? shouldSkipTitle)
    {
        var page = new Page
        {
            title = string.Empty,
            id = null,
            revision = new Revision
            {
                text = new Text { content = string.Empty }
            }
        };

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            switch (reader.LocalName)
            {
                case "title":
                    page.title = reader.ReadElementContentAsString();
                    if (shouldSkipTitle?.Invoke(page.title) == true)
                    {
                        return null;
                    }

                    break;
                case "id" when page.id is null:
                    page.id = reader.ReadElementContentAsString();
                    break;
                case "revision":
                    using (var revisionReader = reader.ReadSubtree())
                    {
                        page.revision = ReadRevision(revisionReader);
                    }

                    break;
            }
        }

        return string.IsNullOrWhiteSpace(page.title) ? null : page;
    }

    private static Revision ReadRevision(XmlReader reader)
    {
        var revision = new Revision
        {
            text = new Text { content = string.Empty }
        };

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "text")
            {
                revision.text.content = reader.ReadElementContentAsString();
                break;
            }
        }

        return revision;
    }
}
