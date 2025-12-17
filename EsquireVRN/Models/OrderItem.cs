using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class OrderItem
    {
        [Key]
        public long ItemID { get; set; }
        public long OrderID { get; set; }
        public long ProdID { get; set; }
        public int ProdQty { get; set; }
        public double Price { get; set; }
        [MaxLength(500)]
        public string? ProdDesc { get; set; }
        [MaxLength(50)]
        public string? ProdCode { get; set; }
        [MaxLength(100)]
        public string? StockCount { get; set; }
        public string? Image { get; set; }
    }
}
