namespace EsquireVRN.Models
{
    public class PackageDetails
    {
        public required string ProductCode { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? Mass { get; set; }
        public decimal? Volume { get; set; }
        public required int ProdQty { get; set; }
    }
}
