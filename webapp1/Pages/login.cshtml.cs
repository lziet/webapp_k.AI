using webapp1.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using webapp1.Services;

namespace webapp1.Pages
{
    public class loginModel : PageModel
    {
        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }

        public string Message { get; set; }

        private readonly AppSettingConfig _config;

        public loginModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using var httpClient = new HttpClient();

            var loginPayload = new
            {
                Username,
                Password
            };

            var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{_config.ApiBaseURL}/api/Users/Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    Message = "Login failed: " + await response.Content.ReadAsStringAsync();
                    return Page();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
                {
                    if (dataElement.TryGetProperty("accessToken", out JsonElement tokenElement) &&
                        dataElement.TryGetProperty("refreshToken", out JsonElement refreshTokenElement))
                    {
                        string token = tokenElement.GetString();
                        string refreshToken = refreshTokenElement.GetString();

                        Response.Cookies.Append("AuthToken", token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                        });

                        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddDays(7)
                        });

                        TempData["Username"] = Username;
                        TempData["Password"] = Password;

                        return RedirectToPage("/Main");
                    }
                    else
                    {
                        Message = "Login response missing token information.";
                        return Page();
                    }
                }
                else
                {
                    Message = "Unexpected login response format.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Message = "An error occurred: " + ex.Message;
                return Page();
            }
        }

    }
}
