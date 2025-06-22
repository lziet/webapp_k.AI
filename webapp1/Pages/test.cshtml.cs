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
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");

            if (Ratings == null || Ratings.Count != 25)
            {
                Message = $"❌ You must answer all 25 questions. You answered {Ratings?.Count ?? 0}.";
                return await OnGetAsync(); // re-fetch questions
            }

            var payload = new { Ratings };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync($"{_config.ApiBaseURL}/api/Transcript", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Message = $"❌ Failed: {responseBody}";
                return await OnGetAsync();
            }

            // ✅ Store a flag to trigger popup on next page load
            TempData["ShowSuccess"] = true;
            return RedirectToPage("/test"); // reload page to trigger popup
        }
    }
}
