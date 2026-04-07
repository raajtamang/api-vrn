using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ContentPageFAQ
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public required string Question { get; set; }
        [Required]
        public required string Answer { get; set; }
        [Required]
        public long PageId { get; set; }
        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime Updated_Date { get; set; }
    }
}
