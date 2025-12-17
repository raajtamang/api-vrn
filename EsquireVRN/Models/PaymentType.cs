using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class PaymentType
    {
        [Key]
        public Byte PayTypeID { get; set; }
        [MaxLength(50)]
        public string? PayDescription { get; set; }
    }
}
