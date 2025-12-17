using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class WishList
    {
        [Key]
        public long WishID { get; set; }
        public long? CustID { get; set; }
        public long ProdID { get; set; }
        public DateTime CreationDate { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
        public string? ImgURL { get; set; }
        public decimal? Rating { get; set; }
        public string? SubCategory { get; set;}
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public Product? Product { get; set; }

    }
}
