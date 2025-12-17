using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class CustomerRegistrationModel
    {
        public long OrgID { get; set; }
        public string? FirstName { get; set; }
        public string? Surname { get; set; }
        public string? Tel { get; set; }
        public string? Tel2 { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Password { get; set; }
        public string? Title { get; set; }
        public long? AccountID { get; set; }
        public string? IdNo { get; set; }
        public string? VatNo { get; set; }
        public string? SendEmails { get; set; }
        public string? DefaultOrgBranchID { get; set; }
        public string? Notes { get; set; }

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
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
    }
}
