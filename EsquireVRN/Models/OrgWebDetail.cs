using System.ComponentModel.DataAnnotations;
using static EsquireVRN.Utils.Shared;

namespace EsquireVRN.Models
{
    public class OrgWebDetail
    {
        public string? OrgName { get; set; }
        public string? WEBEMailInfo { get; set; }
        public string? WEBEMailOrders { get; set; }
        public string? WEBOrgURL { get; set; }
        [Required]
        public required string WEBPriceUsed { get; set; }
        [Required]
        public required string WEBCustPriceUsed { get; set; }
        public string? WEBStockOnly { get; set; }
        public string? isFranchise { get; set; }
        public string? WEBMinStock { get; set; }
        public bool? WEBNoImg { get; set; }
        public string? WEBUseGroup { get; set; }
        public bool? WEBAutoOrder { get; set; }
        public string? WEBProdOrderBy { get; set; }
        public string? OrgRegNo { get; set; }
        public string? OrgVATNo { get; set; }
        [Required]
        public required string OrgTel1 { get; set; }
        public string? OrgTel2 { get; set; }
        public string? OrgFax { get; set; }
        public string? OrgStreet1 { get; set; }
        public string? OrgStreet2 { get; set; }
        public string? OrgStreet3 { get; set; }
        public string? OrgStreet4 { get; set; }
        public string? OrgStreet5 { get; set; }
        public string? OrgProvince { get; set; }
        public bool? VATRegistered { get; set; }
        public FinType FinType { get; set; }
        //public string FirstUserID { get; set; }
        [Required]
        public required double OrgLength { get; set; }
        [Required]
        public required double OrgWidth { get; set; }
        [Required]
        public required double OrgHeight { get; set; }
        [Required]
        public required double OrgMass { get; set; }
        [Required]
        public required decimal Margin { get; set; }
    }
}
