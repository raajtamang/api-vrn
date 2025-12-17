using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ProductSpecification
    {
        [Key]
        public long SpecificationID { get; set; }
        public long ProdID { get; set; }
        public string? Description { get; set; }
        public string? SpecificationValue { get; set; }
    }
}
