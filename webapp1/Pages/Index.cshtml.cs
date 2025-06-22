using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using System;

namespace webapp1.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsLoggedIn { get; private set; }

        public void OnGet()
        {
            var token = Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var jwt = handler.ReadJwtToken(token);
                    var exp = jwt.Payload.Exp;

                    if (exp.HasValue)
                    {
                        var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
                        if (expiryDate > DateTimeOffset.UtcNow)
                        {
                            IsLoggedIn = true;
                        }
                    }
                }
                catch
                {
                    // Invalid token format or expired
                    IsLoggedIn = false;
                }
            }
        }
    }
}
