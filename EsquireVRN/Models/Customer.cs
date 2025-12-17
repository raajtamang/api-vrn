using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Customer
    {
        [Key]
        public long CustID { get; set; }
        public long? OrgID { get; set; }
        public long? AccountID { get; set; }
        public string? FirstName { get; set; }
        public string? Surname { get; set; }
        public string? Tel { get; set; }
        public string? Tel2 { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? PostalAdd { get; set; }
        public string? PostalCode { get; set; }
        public DateTime? DateCreated { get; set; }
        public string? Title { get; set; }
        public string? CellNo { get; set; }
        public string? Notes { get; set; }
        public string? PostalType { get; set; }
        public string? PostalCountry { get; set; }
        public string? PostalAddressIEID { get; set; }
        public string? IdNo { get; set; }
        public string? VatNo { get; set; }
        public int? SendEmails { get; set; }
        public string? ReferenceCode { get; set; }
        public bool IsCommissionActive { get; set; }
        public int? TimesToUseCommission { get; set; }
        public float? AccountCommissionPercentage { get; set; }
        public int? CustomerDiscountPriceNo { get; set; }
        public float CustomerDiscountPercentage { get; set; }
        public int? ReferenceMarketingJourneyId { get; set; }
        public bool CommissionOnProfit { get; set; }
        public bool Active { get; set; }
        public long? FraudulentUserID { get; set; }
        public float? TotalCommissionPercentage { get; set; }
        public string? UserType { get; set; }
        public string? BankName { get; set; }
        public string? AccountType { get; set; }
        public string? AccountNo { get; set; }
        public string? BranchNo { get; set; }
        public long? DefaultOrgBranchID { get; set; }
        public string? CDTown { get; set; }
        public string? Password { get; set; }
        public bool? MarkFradulent { get; set; }
        public string? AccountNumber { get; set; }
    }
}
