using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class OrderWithCustomer
    {
        [Key]
        public long ResellerOrderID { get; set; }
        public long UserID { get; set; }
        public DateTime OrderdDate { get; set; }
        public bool Accepted { get; set; }
        public bool Ordered { get; set; }
        public DateTime DateCreated { get; set; }
        public long WEBOrderID { get; set; }
        public string? DeliveryQuoteID { get; set; }
        public decimal DeliveryCost { get; set; }
        public long DeliveryWaybillID { get; set; }
        public string? DiscountVoucher { get; set; }
        public decimal? Discount { get; set; }
        public long CustomerID { get; set; }
        public long ShippingID { get; set; }
        public long NearestBranchId { get; set; }
        public decimal TotalAmountExcl { get; set; }
        public bool? Rejected { get; set; }
        public string? Rejection_Reason { get; set; }
        public string? DeliveryMethod { get; set; }
        public int DeliveryDescID { get; set; }
        public string? Notes { get; set; }
        public string? ShippingInstruction { get; set; }
        public long? PayId { get; set; }
        public long OrgId { get; set; }
        public string? Customer { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? AccountNo { get; set; }
        public decimal? TotalAmount { get; set; } = 0;
    }
}
