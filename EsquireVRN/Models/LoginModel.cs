namespace EsquireVRN.Models
{
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? timezone { get; set; }
        public string? localtime { get; set; }
    }
}
