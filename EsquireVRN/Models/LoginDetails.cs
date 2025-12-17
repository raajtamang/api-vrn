
namespace EsquireVRN.Models
{
    public class LoginDetails
    {
        public bool IsLoggedIn;
        public string? UserName;
        public string? CustID;
        public string? AccountID;
        public int UsePrice;
        public string? FirstName;
        public string? Surname;
        public string? AccountNumber { get; set; }
        public string? DefaultBranch { get; set; }
        public string Email { get; set; }
    }
}
