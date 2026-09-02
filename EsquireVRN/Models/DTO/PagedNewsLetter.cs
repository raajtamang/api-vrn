namespace EsquireVRN.Models.DTO
{
    public class PagedNewsLetter
    {
        public List<NewsLetter>? NewsLetters { get; set; }
        public long PageCount { get; set; }
    }
}
