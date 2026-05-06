namespace EsquireVRN.Models.DTO
{
    public class PagedCategoriesDTO
    {
        public long page_count { get; set; } = 1;
        public List<CategoryDTO>? Categories { get; set; }
    }
}
