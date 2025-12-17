using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class WebDeliveryMethods
    {
        [Key]
        public long DeliveryID { get; set; }
        public  string? Area { get; set; }
    }
}
