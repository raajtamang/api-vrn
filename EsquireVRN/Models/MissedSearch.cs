using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class MissedSearch
    {
        [Key]
        public long Id { get; set; }
        public string? IP { get; set; }
        public string? SearchString { get; set; }
        public long? CustID { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.AddHours(2).AddMinutes(45);
    }
}
