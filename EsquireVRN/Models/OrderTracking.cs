using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class OrderTracking
    {
        [Key]
        public long Id { get; set; }
        public DateTime ChangeDateTime { get; set; }
        public string Status { get; set; }
    }
}
