using System.IdentityModel.Tokens.Jwt;
using System.Linq;

public static class JwtHelper
{
    public static string? GetUserIdFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return null;

        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
    }
}
