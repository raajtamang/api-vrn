using EsquireVRN.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsquireVRN.Models
{
    public class Product
    {
        [Key]
        public long ProdID { get; set; }
        public long OrgID { get; set; }
        public string ProductCode { get; set; }
        public long? ManufID { get; set; }
        public string? ManufCode { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public double? PurchasePrice { get; set; }
        public double? Price { get; set; }
        public double? DiscountPrice { get; set; }
        public double? PublicPrice { get; set; }
        public string? GroupName { get; set; }
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
        public double? Special_Price { get; set; }
        [SwaggerSchema(ReadOnly = true)]
        public List<BranchStock>? BrancStocks { get; set; }
        public double Rating { get;set; }
        public bool? Active { get;set; }

    }
}
