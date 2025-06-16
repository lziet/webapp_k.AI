using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp1.Pages
{
    public class privacyModel : PageModel
    {
        private readonly ILogger<privacyModel> _logger;

        public privacyModel(ILogger<privacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
