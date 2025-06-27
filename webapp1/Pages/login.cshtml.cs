using webapp1.Models;
using webapp1.Helpers;
using webapp1.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace webapp1.Pages
{
    public class loginModel : PageModel
    {
        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }

        public string Message { get; set; }

        private readonly AppSettingConfig _config;
        private readonly TokenManager _tokenManager;

        public loginModel(IOptions<AppSettingConfig> config, TokenManager tokenManager)
        {
            _config = config.Value;
            _tokenManager = tokenManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WebClient/1.0");

            var loginPayload = new
            {
                username = Username,
                password = Password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginPayload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync($"{_config.ApiBaseURL}/api/users/login", content);
            var json = await response.Content.ReadAsStringAsync();
            ;

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    Message += "\n✅ 'data' found.";

                    // Safely check inner structure
                    if (dataElement.TryGetProperty("accessToken", out var at) &&
                        dataElement.TryGetProperty("refreshToken", out var rt))
                    {
                        ViewData["AccessToken"] = at.GetString();
                        ViewData["RefreshToken"] = rt.GetString();
                        Message += "\n✅ Tokens extracted.";
                    }
                    else
                    {
                        Message += "\n❌ Tokens missing inside 'data'.";
                    }
                }
                else
                {
                    Message += "\n❌ 'data' property not found.";
                }
            }
            catch (Exception ex)
            {
                Message += $"\nException during JSON parsing: {ex.Message}";
            }

            return Page();
        }
    }
}
