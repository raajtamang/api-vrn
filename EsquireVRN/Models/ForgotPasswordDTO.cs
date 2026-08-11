namespace EsquireVRN.Models
{
    public class ForgotPasswordDTO
    {
        public long CustID { get; set; }
        public string? Password { get; set; }
        public string? Salt { get; set; }
        public string? IV { get; set; }
        public string? Email { get; set; }
        public long OrgID { get; set; }
    }
}
