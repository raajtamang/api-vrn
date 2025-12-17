namespace EsquireVRN.Models
{
    public class SearchProductResult
    {
        public List<Product_View>? Products { get; set; }
        public List<Brand>? Brands { get; set; }
        public List<SubCategory>? SubCategories { get; set; }
    }
}
