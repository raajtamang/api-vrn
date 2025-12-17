namespace EsquireVRN.Models
{
    public class DeliveryDetails
    {
        public int PaymentId { get; set; }
        public long ShippingId { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCountry { get; set; }
        public long DeliveryType { get; set; }
        public string? BillingAddress { get; set; }
        public string? BillingCountry { get; set; }
        public long NearestBranchId { get; set; }
        public decimal DeliveryCharge { get; set; }
        public string? DeliveryText { get; set; }
        public string? CustRef { get; set; }
        public string? DeliveryDescription { get; set; }
        public string? ShippingInstruction { get; set; }
        public string? BillingName { get; set; }
        public string?  BillingEmail{ get; set; }
        public string? BillingPhone { get; set; }
        public string? ShippingName { get; set; }
        public string? ShippingEmail { get; set; }
        public string? ShippingPhone { get; set; }
    }

}
