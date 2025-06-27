using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using webapp1.Helpers;
using webapp1.Services;

public class mainModel : PageModel
{
    private readonly AppSettingConfig _config;

    public string ApiBaseUrl { get; set; }

    public mainModel(IOptions<AppSettingConfig> config)
    {
        _config = config.Value;
    }

    public void OnGet()
    {
        ApiBaseUrl = _config.ApiBaseURL; // Use _ if you prefer the deployment one
    }
}
