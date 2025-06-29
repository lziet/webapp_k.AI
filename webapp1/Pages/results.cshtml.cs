using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using webapp1.Services;

namespace webapp1.Pages
{
    public class resultsModel : PageModel
    {
        private readonly AppSettingConfig _config;

        public string ApiBaseUrl { get; set; }

        public resultsModel(IOptions<AppSettingConfig> config)
        {
            _config = config.Value;
            ApiBaseUrl = _config.ApiBaseURL;
        }

        public void OnGet()
        {
            // ApiBaseUrl passed to page for JS
        }
    }
}
