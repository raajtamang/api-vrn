using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class SpecialPageProduct
    {
        [Key]
        public long Id { get; set; }
        public string ProductCode { get; set; }
        public decimal? OldPrice { get; set; }
        public decimal SpecialPrice { get; set; }
        public DateTime? Date { get; set; }
        public long PageId { get; set; }
        public decimal? Margin { get; set; }
        public string? ImgURL { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? PageType { get; set; }
        public string? Brand { get; set; }
        public string? SubCategory { get; set; }
        public long? Stock { get; set; }
        public long? ProdID { get; set; }
        public string? ProductName { get; set; }
        public bool? Active { get; set; }
        public string? Notes{ get; set; }
    }
}
