namespace EsquireVRN.Models
{
    public class SearchModel
    {
        public string? SearchText { get; set; }
        public long[]? Categories { get; set; }
        public long[]? Brands { get; set; }
        public decimal? Minimum_Price { get; set; }
        public decimal? Maximum_Price { get; set;}
        public int? Rating { get; set;}
        public int? Page_Number { get; set; }
        public int? Page_Size { get; set; }
    }
}
