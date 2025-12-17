using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class NewsLetter
    {
        [Key]
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public DateTime? Date { get; set; }
    }
}
