using static EsquireVRN.Utils.Shared;

namespace EsquireVRN.Models
{
    public class OrgWebDetail
    {
        public string? OrgName { get; set; }
        public string? WEBEMailInfo { get; set; }
        public string? WEBEMailOrders { get; set; }
        public string? WEBOrgURL { get; set; }
        public string? WEBPriceUsed { get; set; }
        public string? WEBCustPriceUsed { get; set; }
        public string? WEBStockOnly { get; set; }
        public string? isFranchise { get; set; }
        public string? WEBMinStock { get; set; }
        public bool? WEBNoImg { get; set; }
        public string? WEBUseGroup { get; set; }
        public bool? WEBAutoOrder { get; set; }
        public string? WEBProdOrderBy { get; set; }
        public string OrgRegNo { get; set; }
        public string OrgVATNo { get; set; }
        public string OrgTel1 { get; set; }
        public string? OrgTel2 { get; set; }
        public string OrgFax { get; set; }
        public string OrgStreet1 { get; set; }
        public string? OrgStreet2 { get; set; }
        public string? OrgStreet3 { get; set; }
        public string? OrgStreet4 { get; set; }
        public string? OrgStreet5 { get; set; }
        public string? OrgProvince { get; set; }
        public bool VATRegistered { get; set; }
        public string? FromDoorID { get; set; }
        public FinType FinType { get; set; }
        //public string FirstUserID { get; set; }
        public double? OrgLength { get; set; }
        public double? OrgWidth { get; set; }
        public double? OrgHeight { get; set; }
        public double? OrgMass { get; set; }
    }
}
