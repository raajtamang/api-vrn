using System.Text.Json;

namespace EsquireVRN.Utils
{
    public class TurnstileValidator
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public TurnstileValidator(HttpClient httpClient, string secretKey)
        {
            _httpClient = httpClient;
            _secretKey = secretKey;
        }

        public async Task<bool> ValidateAsync(string token, string? userIp = null)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var form = new Dictionary<string, string>
        {
            { "secret", _secretKey },
            { "response", token }
        };

            if (!string.IsNullOrEmpty(userIp))
            {
                form.Add("remoteip", userIp);
            }

            var response = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(form)
            );

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<TurnstileResponse>(json);

            return result?.success == true;
        }
    }

    public class TurnstileResponse
    {
        public bool success { get; set; }
        public string[] error_codes { get; set; }
        public string challenge_ts { get; set; }
        public string hostname { get; set; }
    }
}
