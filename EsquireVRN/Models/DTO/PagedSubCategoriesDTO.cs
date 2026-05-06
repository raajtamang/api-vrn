namespace EsquireVRN.Models.DTO
{
    public class PagedSubCategoriesDTO
    {
        public long page_count { get; set; } = 1;
        public long CategoryID { get; set; }
        public List<CategoryDTO>? SubCategories { get; set; }
    }
}
