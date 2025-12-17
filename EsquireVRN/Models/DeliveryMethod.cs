using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class DeliveryMethod
    {
        [Key]
        public int DeliveryID { get; set; }
        public string? Area { get; set; }
        public double RuleAmount { get; set; }
    }
}
