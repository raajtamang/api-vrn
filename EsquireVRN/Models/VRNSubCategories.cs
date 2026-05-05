using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class VRNSubCategories
    {
        [Key]
        public long Id { get; set; }
        public long SubCategoryId { get; set; }
        public long OrgId { get; set; }
        public long Position { get; set; } = 1;
        public long CategoryID { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
