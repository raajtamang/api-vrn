using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class BankDetails
    {
        [Key]
        public long BankID { get; set; }
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public long AccountNo { get; set; }
        public long OrgID { get; set; }
        public long OrgBranchID { get; set; }
        public string? AccountName { get; set; }
        public int BankNameId { get; set; }
        public int BankAccountTypeId { get; set; }

    }
}
