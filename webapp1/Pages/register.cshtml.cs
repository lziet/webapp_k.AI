using webapp1.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using webapp1.Services;
using System.Text.Json;

namespace webapp1.Pages
{
    public class registerModel : PageModel
    {
        [BindProperty] public string FullName { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string PhoneNumber { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }

        public string Message { get; set; }

        // Giả lập lưu trữ người dùng trong RAM
        public static Dictionary<string, UserInfo> Users = new();

        public class UserInfo
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Address { get; set; }
            public string Password { get; set; }
        }

        private readonly AppSettingConfig _config;

        public registerModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
        }
        public async Task<IActionResult> OnPostAsync()
        {
            using var httpClient = new HttpClient();

            var registerPayload = new
            {
                FullName,
                Email,
                Phone = PhoneNumber,
                Address,
                Username,
                Password
            };

            var registerContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            try
            {
                // Step 1: Register
                var regResponse = await httpClient.PostAsync($"{_config.ApiBaseURL}/Users/Create", registerContent);

                if (!regResponse.IsSuccessStatusCode)
                {
                    var regError = await regResponse.Content.ReadAsStringAsync();
                    Message = $"Registration failed: {regError}";
                    return Page();
                }

                // Step 2: Login
                var loginPayload = new { Username, Password };
                var loginContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(loginPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var loginResponse = await httpClient.PostAsync($"{_config.ApiBaseURL}/api/Users/Login", loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var loginError = await loginResponse.Content.ReadAsStringAsync();
                    Message = $"Login failed after registration: {loginError}";
                    return Page();
                }

                var loginJson = await loginResponse.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(loginJson);

                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.TryGetProperty("accessToken", out JsonElement accessTokenElement) &&
                    dataElement.TryGetProperty("refreshToken", out JsonElement refreshTokenElement))
                {
                    var accessToken = accessTokenElement.GetString();
                    var refreshTokenValue = refreshTokenElement.GetString(); // renamed to avoid CS0136

                    Response.Cookies.Append("AuthToken", accessToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                    });

                    Response.Cookies.Append("RefreshToken", refreshTokenValue, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });

                    return RedirectToPage("/Main");
                }
                else
                {
                    Message = "Login response missing token information.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Message = $"Error occurred: {ex.Message}";
                return Page();
            }
        }


    }
}