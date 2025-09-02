using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Books;

public sealed class BookCollectionDto : BaseDto
{
    public BookCollectionDto()
    {
        ObjectType = ObjectTypeEnum.Book;
    }
    
    public int? Quality { get; set; }
    public string? RegionLore { get; set; }
    public string? RegionLocation { get; set; }
    public int? VolumeCount { get; set; }
    public string? Author { get; set; }

    // Aquisições por volume (1..N)
    public Dictionary<int, List<string>>? AcquisitionByVolume { get; set; }

    // Conteúdo por volume
    public List<BookVolumeDto>? Volumes { get; set; }
}