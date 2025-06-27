using Microsoft.AspNetCore.Http;
using System;
using webapp1.Models;

namespace webapp1.Services
{
    public class TokenManager
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public TokenManager(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void StoreTokens(TokenModel tokens)
        {
            var context = _contextAccessor.HttpContext;
            if (context == null || tokens == null) return;

            context.Response.Cookies.Append("AuthToken", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });

            context.Response.Cookies.Append("RefreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        public TokenModel? GetTokens()
        {
            var context = _contextAccessor.HttpContext;
            if (context == null) return null;

            var access = context.Request.Cookies.TryGetValue("AuthToken", out var accessToken);
            var refresh = context.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken);

            if (!access || !refresh || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                return null;

            return new TokenModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public void ClearTokens()
        {
            var context = _contextAccessor.HttpContext;
            context?.Response.Cookies.Delete("AuthToken");
            context?.Response.Cookies.Delete("RefreshToken");
        }
    }
}
