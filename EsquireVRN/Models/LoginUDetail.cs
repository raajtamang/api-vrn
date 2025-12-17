namespace EsquireVRN.Models
{
    public class LoginUDetail
    {
        public long OrgID { get; set; }
        public long CustID { get; set; }
        public long AccountID { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public bool Active { get; set; }
        public int UsePrice { get; set; }
        public string? DefaultBranch { get; set; }
        public string? AccountNo { get; set; }

    }
}
