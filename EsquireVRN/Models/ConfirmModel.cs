namespace EsquireVRN.Models
{
    public class ConfirmModel
    {
        public int PaymentId { get; set; }
        public long ShippingId { get; set; }
        public string? SessionID { get; set; }
        public long NearestBranchId { get; set; }
        public long DeliveryType { get; set; }
        public decimal DeliveryCharge { get; set; }
        public string? DeliveryText { get; set; }
        public string? CustRef { get; set; }
        public string? DeliveryQuoteId { get; set; }
        public string? ShippingInstruction { get; set; }
    }
}
