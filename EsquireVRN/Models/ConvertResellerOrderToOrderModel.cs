using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class ConvertResellerOrderToOrderModel
    {
        [Required]
        public required long ResellerOrderId { get; set; }
        public int? PaymentId { get; set; }
    }
}
