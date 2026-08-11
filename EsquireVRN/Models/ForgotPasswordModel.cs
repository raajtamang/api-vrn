using System.Text.Json.Serialization;

namespace EsquireVRN.Models
{
    public class ForgotPasswordModel
    {
        public required string email { get; set; }

        [JsonPropertyName("cf-turnstile-response")]
        public required string CfTurnstileResponse { get; set; }
    }
}
