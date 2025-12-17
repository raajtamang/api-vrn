using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class MissedSearchDTO
    {
        [Key]
        public long Id { get; set; }
        public string? IP { get; set; }
        public string? SearchString { get; set; }
        public long? CustID { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? AccountNo { get; set; }
        public DateTime Date { get; set; }
    }
}
