namespace Genshin.Wiki.Parser.Models.Books;

public sealed class BookVolumeDto
{
    public int Index { get; set; }                 // 1..N
    public string? Description { get; set; }       // do {{Description|...}}
    public string? Text { get; set; }              // corpo limpo do volume
}