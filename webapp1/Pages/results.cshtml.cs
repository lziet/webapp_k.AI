using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using webapp1.Services;

namespace webapp1.Pages
{
    public class resultsModel : PageModel
    {
        public class Transcript
        {
            public int Id_transcript { get; set; }
            public string Content { get; set; } = "";
            public DateTime Date { get; set; }
        }

        public class Question
        {
            public int Id_question { get; set; }
            public string Content { get; set; } = "";
        }

        public List<Transcript> Transcripts { get; set; } = new();
        public List<Question> Questions { get; set; } = new();
        public string Message { get; set; }

        private readonly AppSettingConfig _config;

        public resultsModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // Get transcripts
                var userId = JwtHelper.GetUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login");

                var transcriptResponse = await client.GetAsync($"{_config.ApiBaseURL}/api/transcript/{userId}");
                if (!transcriptResponse.IsSuccessStatusCode)
                {
                    Message = "Failed to fetch transcripts.";
                    return Page();
                }

                var transcriptJson = await transcriptResponse.Content.ReadAsStringAsync();
                Transcripts = JsonSerializer.Deserialize<List<Transcript>>(transcriptJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

                // Get questions
                var questionResponse = await client.GetAsync($"{_config.ApiBaseURL}/questions");
                if (!questionResponse.IsSuccessStatusCode)
                {
                    Message = "Failed to fetch questions.";
                    return Page();
                }

                var questionJson = await questionResponse.Content.ReadAsStringAsync();
                Questions = JsonSerializer.Deserialize<List<Question>>(questionJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            return Page();
        }

        public string GetAnswerForQuestion(string content, int questionNumber)
        {
            try
            {
                int index = (questionNumber - 1) * 3;
                if (index + 2 >= content.Length) return "?";
                return content.Substring(index, 3); // "01X", "02X", ...
            }
            catch
            {
                return "?";
            }
        }

        public string GetAnswerDescription(string code)
        {
            return code.Length == 3 && int.TryParse(code.Substring(2, 1), out int option) ? option switch
            {
                1 => "Disagree",
                2 => "Slightly disagree",
                3 => "Neutral",
                4 => "Slightly agree",
                5 => "Agree",
                _ => "Unknown"
            } : "Invalid";
        }
    }
}
