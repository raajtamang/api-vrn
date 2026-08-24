using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class OrgCategory
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public long OrgId { get; set; }
        [Required]
        public required string Category { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.AddHours(2);
    }
}
