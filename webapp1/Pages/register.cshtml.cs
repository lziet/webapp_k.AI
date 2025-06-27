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
using System;

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

        private readonly AppSettingConfig _config;
        private readonly TokenManager _tokenManager;

        public registerModel(IOptions<AppSettingConfig> config, TokenManager tokenManager)
        {
            _config = config.Value;
            _tokenManager = tokenManager;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using var httpClient = new HttpClient();

            // ✅ Add required headers
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WebClient/1.0");

            // ✅ Use UserRegisterDto for registration payload
            var registerPayload = new UserRegisterDto
            {
                Username = Username,
                Password = Password,
                FullName = FullName,
                Email = Email,
                Phone = PhoneNumber,
                Address = Address
            };

            var registerContent = new StringContent(
                JsonSerializer.Serialize(registerPayload),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // ✅ Step 1: Send registration request
                var regResponse = await httpClient.PostAsync($"{_config.ApiBaseURL}/users", registerContent);

                if (!regResponse.IsSuccessStatusCode)
                {
                    var regError = await regResponse.Content.ReadAsStringAsync();
                    Message = $"Registration failed: {regError}";
                    return Page();
                }

                // ✅ Step 2: Login with same credentials using UserLoginDto
                var loginPayload = new UserLoginDto
                {
                    Username = Username,
                    Password = Password
                };

                var loginContent = new StringContent(
                    JsonSerializer.Serialize(loginPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var loginResponse = await httpClient.PostAsync($"{_config.ApiBaseURL}/api/users/login", loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var loginError = await loginResponse.Content.ReadAsStringAsync();
                    Message = $"Login failed after registration: {loginError}";
                    return Page();
                }

                var loginJson = await loginResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(loginJson);

                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    var tokenModel = JsonSerializer.Deserialize<TokenModel>(dataElement.ToString());

                    if (tokenModel != null && !string.IsNullOrEmpty(tokenModel.AccessToken))
                    {
                        _tokenManager.StoreTokens(tokenModel);
                        return RedirectToPage("/Main");
                    }

                    Message = "Login response missing or invalid token.";
                    return Page();
                }

                Message = "Unexpected login response format.";
                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Error occurred: {ex.Message}";
                return Page();
            }
        }
    }
}
