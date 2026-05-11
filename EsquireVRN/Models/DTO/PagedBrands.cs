namespace EsquireVRN.Models.DTO
{
    public class PagedBrands
    {
        public long page_count { get; set; } = 1;
        public List<Brand>? Brands { get; set; }
    }
}
