using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ProductFeature
    {
        [Key]
        public long FeatureID { get; set; }
        public long ProdID { get; set; }
        public string? Description { get; set; }
        public string? FeatureValue { get; set; }
    }
}
