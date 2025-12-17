using EsquireVRN.Models;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Product_View
    {
        [Key]
        public long ProdID { get; set; }
        public string ProductCode { get; set; }
        public string LongDescription { get; set; }
        public string URL { get; set; }
        public string ImgURL { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal PublicPrice { get; set; }
        public string ManufacturerName { get; set; }
        public string GroupName { get; set; }
        public string OrgName { get; set; }
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
        public int StockQty { get; set; }
        public double Rating { get; set; }
        public string? ProductName { get; set; }
        public double? Special_Price { get; set; }
        public long? ManufID { get; set; }
        public List<BranchStock>? BrancStocks { get; set; }
    }
}
