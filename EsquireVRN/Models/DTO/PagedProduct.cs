namespace EsquireVRN.Models
{
    public class PagedProduct
    {
        public List<Product_View>? Products { get; set; }
        public long PageCount { get; set; }
    }
}
