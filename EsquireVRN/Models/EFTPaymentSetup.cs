using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class EFTPaymentSetup
    {
        [Key]
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? AccountNo { get; set; }
        public string? BranchCode { get; set; }
        public string? AccountName { get; set; }
        public string? LogoUrl { get; set; }
        public long OrgID { get; set; }
        public int Position { get; set; } = 0;
    }
}
