using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
        public string ApiBaseUrl => _config.ApiBaseURL;

        private readonly AppSettingConfig _config;

        public testModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{_config.ApiBaseURL}/api/questions");

            if (!response.IsSuccessStatusCode)
                return RedirectToPage("/Login");

            var json = await response.Content.ReadAsStringAsync();
            Questions = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            return Page();
        }
    }
}
