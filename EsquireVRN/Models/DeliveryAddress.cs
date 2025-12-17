using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class DeliveryAddress
    {
        [Key]
        public long ShippingID { get; set; }
        [MaxLength(100)]
        public string? ShippingDesc { get; set; }
        [MaxLength(400)]
        public string? ShippingAddress { get; set; }
        [MaxLength(50)]
        public string? ShippingCountry { get; set; }
        [MaxLength(30)]
        public string? ShippingType { get; set; }
        public long ShippingddressIEID { get; set; }
        [MaxLength(100)]
        public string? CourierDirectKey { get; set; }
        [MaxLength(150)]
        public string? Town { get; set; }
        public long CustID { get; set; }
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
