using EsquireVRN.Models;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class OrderWithCustomer
    {
        [Key]
        public long OrderID { get; set; }
        public long CustID { get; set; }
        public DateTime OrderDate { get; set; }
        public string? DeliveryMethod { get; set; }
        public int? DeliveryDescID { get; set; }
        public decimal? DeliveryCost { get; set; }
        public long PayID { get; set; }
        public long StatusID { get; set; }
        public string? Notes { get; set; }
        public long ShippingID { get; set; }
        public long OrgID { get; set; }
        public long OrgBranchID { get; set; }
        public bool Insurance { get; set; }
        public string? DeliveryQuoteID { get; set; }
        public short DistOrdStatus { get; set; }
        public bool ReviewEmailSent { get; set; }
        public long DeliveryWaybillID { get; set; }
        public string? CustRef { get; set; }
        public string? DiscountRefCode { get; set; }
        public decimal Discount { get; set; }
        public long DeliveryID { get; set; }
        public long? FinconId { get; set; }
        public string? Customer { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? AccountNo { get; set; }
        public decimal? TotalAmount { get; set; }
        public String PaymentReference { get; set; }
    }
}
