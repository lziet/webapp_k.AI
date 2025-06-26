using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using webapp1.Services;
using System.IdentityModel.Tokens.Jwt;

namespace webapp1.Pages
{
    public class testModel : PageModel
    {
        public class Question
        {
            public int Id_question { get; set; }
            public string Content { get; set; } = string.Empty;
            public int Score { get; set; }
        }

        public List<Question> Questions { get; set; } = new();

        [BindProperty]
        public List<int> Ratings { get; set; } = new();

        public string Message { get; set; }

        private readonly AppSettingConfig _config;

        public testModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("https://localhost:7105/Questions/GetAllQuestions");

            if (!response.IsSuccessStatusCode) return RedirectToPage("/Login");

            var json = await response.Content.ReadAsStringAsync();
            Questions = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string token = Request.Cookies["AuthToken"];
            string refreshToken = Request.Cookies["RefreshToken"];

            if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var exp = jwt.Payload.Exp;

                if (exp.HasValue)
                {
                    var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
                    if (expiryDate <= DateTimeOffset.UtcNow)
                    {
                        // Token expired - try to renew
                        if (string.IsNullOrEmpty(refreshToken)) return RedirectToPage("/Login");

                        using var refreshClient = new HttpClient();
                        var refreshPayload = new
                        {
                            accessToken = token,
                            refreshToken = refreshToken
                        };

                        var refreshContent = new StringContent(JsonSerializer.Serialize(refreshPayload), Encoding.UTF8, "application/json");
                        var refreshResponse = await refreshClient.PostAsync($"{_config.ApiBaseURL}/api/Users/RenewToken", refreshContent);

                        if (!refreshResponse.IsSuccessStatusCode)
                            return RedirectToPage("/Login");

                        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(refreshJson);

                        if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("accessToken", out JsonElement newAccessTokenElement) &&
                            dataElement.TryGetProperty("refreshToken", out JsonElement newRefreshTokenElement))
                        {
                            token = newAccessTokenElement.GetString();
                            refreshToken = newRefreshTokenElement.GetString();

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
                        }
                        else
                        {
                            return RedirectToPage("/Login");
                        }
                    }
                }
            }
            catch
            {
                return RedirectToPage("/Login");
            }

            // Proceed to send Ratings
            var payload = new { Ratings };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync($"{_config.ApiBaseURL}/api/Transcript", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"DEBUG - StatusCode: {response.StatusCode}, Body: {responseBody}");
                Message = $"❌ Failed: {responseBody}";
                return await OnGetAsync();
            }

            TempData["ShowSuccess"] = true;
            return RedirectToPage("/test");
        }

    }
}
