using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using webapp1.Services;

public class registerModel : PageModel
{
    private readonly AppSettingConfig _config;
    public string ApiBaseUrl => _config.ApiBaseURL;

    public registerModel(IOptions<AppSettingConfig> config)
    {
        _config = config.Value;
    }
}
