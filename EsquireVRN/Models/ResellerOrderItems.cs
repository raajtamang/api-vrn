using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ResellerOrderItems
    {
        [Key]
        public long ItemID { get; set; }
        public long ResellerOrderID { get; set; }
        public long ProdID { get; set; }
        public int ProdQty { get; set; }
        public double Price { get; set; }
        public string? ProdDesc { get; set; }
        public required string ProdCode { get; set; }
    }
}
