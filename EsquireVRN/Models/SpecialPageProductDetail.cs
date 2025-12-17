using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class SpecialPageProductDetail
    {
        [Key]
        public long Id { get; set; }
        public string ProductCode { get; set; }
        public decimal? OldPrice { get; set; }
        public decimal SpecialPrice { get; set; }
        public DateTime? Date { get; set; }
        public long PageId { get; set; }
        public decimal? Margin { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? PageType { get; set; }
        public long ProdID { get; set; }
        public long OrgID { get; set; }
        public string Brand { get; set; }
        public string ManufID { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public double? PurchasePrice { get; set; }
        public double? Price { get; set; }
        public double? DiscountPrice { get; set; }
        public double? PublicPrice { get; set; }
        public string? SubCategory { get; set; }
        public string? UsualAvailability { get; set; }
        public string? Notes { get; set; }
        public string? URL { get; set; }
        public string? ImgURL { get; set; }
        public int? Status { get; set; }
        public int? Warranty { get; set; }
        public long? OrgSourceID { get; set; }
        public int? StockQty { get; set; }
        public int? DiscQty { get; set; }
        public string? Unit { get; set; }
        public DateTime? CreateDate { get; set; }
        public double? Length { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Mass { get; set; }
        public long? DebitOrderFormId { get; set; }
        public long? DeliveryID { get; set; }
        public long? MasterProdID { get; set; }
        public bool? AdwordExclude { get; set; }
        public int? DataSource { get; set; }
        public string? ProductName { get; set; }
        [SwaggerSchema(ReadOnly = true)]
        public List<BranchStock>? BrancStocks { get; set; }
        public bool? Active { get; set; }
    }
}
