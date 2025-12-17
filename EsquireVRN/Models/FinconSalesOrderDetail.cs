namespace EsquireVRN.Models
{
    public class FinconSalesOrderDetail
    {
        public string Entry { get; set; }
        public string DocNo { get; set; }
        public string ItemNo { get; set; }
        public int Quantity { get; set; }
        public double LineTotalExcl { get; set; }
        public string TaxCode { get; set; }
        public double LineTotalTax { get; set; }
        public string Description { get; set; }
        public double UnitCost { get; set; }
        public int Group { get; set; }
    }
}
