using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class PaymentStatus
    {
        [Key]
        public Byte PayStatusID { get; set; }
        [MaxLength(50)]
        public string? PayStatus { get; set; }
    }
}
