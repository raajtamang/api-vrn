using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models.DTO
{
    public class VRNSubCategoryDTO
    {
        [Required]
        public long CategoryID { get; set; }
        [Required]
        public long SubCategoryId { get; set; }
    }
}
