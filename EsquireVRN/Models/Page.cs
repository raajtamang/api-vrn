using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ContentPage
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string? Type { get; set; }
        public long OrgId { get; set; }
        public string? Content { get; set; }
        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime? Updated_Date { get; set; }
    }
}
