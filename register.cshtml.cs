using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using webapp1.Services;

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
                Phone = PhoneNumber, // Fix: match API field name
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
                var regResponse = await httpClient.PostAsync("https://localhost:7105/Users/Create", registerContent);

                if (!regResponse.IsSuccessStatusCode)
                {
                    var regError = await regResponse.Content.ReadAsStringAsync();
                    Message = $"Registration failed: {regError}";
                    return Page();
                }

                // Step 2: Login
                var loginPayload = new
                {
                    Username,
                    Password
                };

                var loginContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(loginPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var loginResponse = await httpClient.PostAsync("https://localhost:7105/api/Users/Login", loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var loginError = await loginResponse.Content.ReadAsStringAsync();
                    Message = $"Login failed after registration: {loginError}";
                    return Page();
                }

                var loginJson = await loginResponse.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(loginJson);
                string token = doc.RootElement.GetProperty("data").GetString();

                // Step 3: Store the token in a cookie
                Response.Cookies.Append("AuthToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });

                return RedirectToPage("/Main");
            }
            catch (Exception ex)
            {
                Message = $"Error occurred: {ex.Message}";
                return Page();
            }
        }

    }
}