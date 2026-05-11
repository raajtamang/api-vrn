namespace EsquireVRN.Models.DTO
{
    public class PagedBrandsDTO
    {
        public long page_count { get; set; } = 1;
        public List<BrandDTO>? Brands { get; set; }
    }
}
