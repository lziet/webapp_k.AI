using webapp1.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System;
using Microsoft.AspNetCore.Mvc;
using webapp1.Services;

public class mainModel : PageModel
{
    public UserDto UserData { get; set; }

    private readonly AppSettingConfig _config;

    public mainModel(IOptions<AppSettingConfig> config)
    {
        _config = config.Value;
    }
    public async Task<IActionResult> OnGetAsync([FromForm] string action)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Login");

        var userId = JwtHelper.GetUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Login");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{_config.ApiBaseURL}/Users/GetById/{userId}");
        if (!response.IsSuccessStatusCode)
            return RedirectToPage("/Login");

        var json = await response.Content.ReadAsStringAsync();
        UserData = JsonSerializer.Deserialize<UserDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        if (action == "logout")
        {
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("RefreshToken");
            UserData = null;
            return RedirectToPage("/Login");
        }

        if (action == "update")
        {
            var token = Request.Cookies["AuthToken"];
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken)) return RedirectToPage("/Login");

            var userId = JwtHelper.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login");

            await OnGetAsync(null); // Get current user info
            var currentData = UserData;

            var form = Request.Form;
            var updatedData = new UserDto
            {
                Username = string.IsNullOrWhiteSpace(form["Username"]) ? currentData.Username : form["Username"],
                Password = string.IsNullOrWhiteSpace(form["Password"]) ? currentData.Password : form["Password"],
                FullName = string.IsNullOrWhiteSpace(form["FullName"]) ? currentData.FullName : form["FullName"],
                Email = string.IsNullOrWhiteSpace(form["Email"]) ? currentData.Email : form["Email"],
                Phone = string.IsNullOrWhiteSpace(form["Phone"]) ? currentData.Phone : form["Phone"],
                Address = string.IsNullOrWhiteSpace(form["Address"]) ? currentData.Address : form["Address"]
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(updatedData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_config.ApiBaseURL}/Users/Update/{userId}", content);

            if (response.IsSuccessStatusCode)
            {
                // 🟢 Update succeeded — now renew token
                var renewPayload = new
                {
                    accessToken = token,
                    refreshToken = refreshToken
                };

                var renewContent = new StringContent(JsonSerializer.Serialize(renewPayload), System.Text.Encoding.UTF8, "application/json");
                var renewResponse = await client.PostAsync($"{_config.ApiBaseURL}/api/Users/RenewToken", renewContent);

                if (renewResponse.IsSuccessStatusCode)
                {
                    var renewJson = await renewResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(renewJson);
                    if (doc.RootElement.TryGetProperty("data", out JsonElement data) &&
                        data.TryGetProperty("accessToken", out JsonElement newTokenElem) &&
                        data.TryGetProperty("refreshToken", out JsonElement newRefreshElem))
                    {
                        var newAccessToken = newTokenElem.GetString();
                        var newRefreshToken = newRefreshElem.GetString();

                        CookieHelper.SetAuthCookies(Response, newAccessToken, newRefreshToken); // ✅ helper method
                    }
                }

                TempData["Message"] = "Cập nhật thành công!";
                return RedirectToPage("/Main");
            }

            TempData["Message"] = "Cập nhật thất bại.";
            return Page();
        }


        return Page();
    }


    public class UserDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

}
