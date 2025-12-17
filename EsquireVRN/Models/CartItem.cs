using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class CartItem
    {
        [Key]
        public long BasketId { get; set; }
        public long? OrgID { get; set; }
        public string? SessionID { get; set; }
        public long ProdID { get; set; }
        public int ProdQty { get; set; }
        public string? ProdDesc { get; set; }
        public decimal Price { get; set; }
        public string? ProdCode { get; set; }
        public long? CustID { get; set; }
        [SwaggerSchema(ReadOnly = true)]
        public string? ImgURL { get; set; }       
        public long? StockQuantity { get; set; }
        public Product? Product { get; set; }
    }
}
