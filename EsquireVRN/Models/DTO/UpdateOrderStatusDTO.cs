using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models.DTO
{
    public class UpdateOrderStatusDTO
    {
        [Required]
        public int StatusId { get; set; }
        [Required]
        public long ResellerOrderID { get; set; }
    }
}
