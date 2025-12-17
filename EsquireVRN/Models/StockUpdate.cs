namespace EsquireVRN.Models
{
    public class StockUpdate
    {
        public string ItemNo { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitCost { get; set; }
        public string? SellingPrice1 { get; set; }
        public string? SellingPrice2 { get; set; }
        public string? SellingPrice3 { get; set; }
        public string? SellingPrice4 { get; set; }
        public string? SellingPrice5 { get; set; }
        public string? SellingPrice6 { get; set; }
        public string? Notes { get; set; }
        public string? Description { get; set; }
        public string? CatDescription { get; set; }
        public string? Brand { get; set; }
        public string? BoxWidth { get; set; }
        public string? BoxHeight { get; set; }
        public string? BoxLength { get; set; }
        public string? Active { get; set; }
        public string? BrandDescription { get; set; }
        public string? Warranty { get; set; }
        public List<StockLoc>? StockLoc { get; set; }
        public string? URL { get; set; }
        public string? Category { get; set; }
    }
}
