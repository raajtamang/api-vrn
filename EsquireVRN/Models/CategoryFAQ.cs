using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class CategoryFAQ
    {
        [Key]
        public long FAQID { get; set; }
        public long CategoryId { get; set; }
        [Required,MaxLength(1000)]
        public required string Question { get; set; }
        [Required]
        public required string Answer { get; set; }
    }
}
