using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ProductReview
    {
        [Key]
        public long ProdRevID { get; set; }
        public long? OrgID { get; set; }
        public long ProdID { get; set; }
        public string? ProdCode { get; set; }
        public long? CustID { get; set; }
        public string? ProdRevHeading { get; set; }
        public int? ProdRevRating { get; set; }
        public string? ProdRevPros { get; set; }
        public string? ProdRevCons { get; set; }
        public string? ProdRevText { get; set; }
        public DateTime ProdRevDate { get; set; }
        public long? RefID { get; set; }
        public int? ReviewStatusID { get; set; }
        public string? Reviewer { get; set; }
        public string? ProductName { get;set; }
    }
}
