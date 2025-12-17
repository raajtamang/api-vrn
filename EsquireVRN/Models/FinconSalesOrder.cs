namespace EsquireVRN.Models
{
    public class FinconSalesOrder
    {
        public string? OrderNo { get; set; }
        public string?   AccNo { get; set; }
        public string? LocNo { get; set; }
        public string? QuoteNo { get; set; }
        public string? OrderType { get; set; }
        public string? OrderDate { get; set; }
        public string? DateRequired { get; set; }
        public string? ExpiryDate { get; set; }
        public double TotalExcl { get; set; }
        public double TotalTax { get; set; }
        public string? CurrencyCode { get; set; }
        public double ExchangeRate { get; set; }
        public string? CustomerRef { get; set; }
        public string? DebName { get; set; }
        public string? Addr1 { get; set; }
        public string? Addr2 { get; set; }
        public string? Addr3 { get; set; }
        public string? PCode { get; set; }
        public string? DelName { get; set; }
        public string? DelAddr1 { get; set; }
        public string? DelAddr2 { get; set; }
        public string? DelAddr3 { get; set; }
        public string? DelAddr4 { get; set; }
        public string? DelPCode { get; set; }
        public string? DelInstruc1 { get; set; }
        public string? DelInstruc2 { get; set; }
        public string? DelInstruc3 { get; set; }
        public string? DelInstruc4 { get; set; }
        public string? DelInstruc5 { get; set; }
        public string? DelInstruc6 { get; set; }
        public char? DeliveryMethod { get; set; }
        public string? RepCode { get; set; }
        public string? TaxNo { get; set; }
        public string? Approved { get; set; }
        public int NumberOfItems { get; set; }
        public string? InvoiceNumbers { get; set; }
        public string? Status { get; set; }
        public List<FinconSalesOrderDetail> SalesOrderDetail { get; set; }
        public FinconSalesOrderPayment SalesOrderPayment { get; set; }
    }
}
