using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class VRNBrands
    {
        [Key]
        public long Id { get; set; }
        public long BrandId { get; set; }
        public long OrgId { get; set; }
        public long Position { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
