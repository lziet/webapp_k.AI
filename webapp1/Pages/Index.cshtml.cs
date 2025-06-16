using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp1.Pages
{
    public class indexModel : PageModel
    {
        private readonly ILogger<indexModel> _logger;

        public indexModel(ILogger<indexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
