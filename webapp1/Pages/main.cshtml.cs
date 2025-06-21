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

        var response = await client.GetAsync($"https://localhost:7105/Users/GetById/{userId}");
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
            UserData = null;
            return RedirectToPage("/Login");
        }

        if (action == "update")
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");

            var userId = JwtHelper.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login");

            // Load existing user data for fallback
            await OnGetAsync(null);
            var currentData = UserData;

            // Read form values
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

            var response = await client.PutAsync($"https://localhost:7105/Users/Update/{userId}", content);

            if (response.IsSuccessStatusCode)
            {
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
