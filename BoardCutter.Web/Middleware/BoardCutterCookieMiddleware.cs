using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BoardCutter.Web.Middleware
{
    public class BoardCutterCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CookieName = "BoardCutter";

        public BoardCutterCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the BoardCutter cookie exists
            if (!context.Request.Cookies.ContainsKey(CookieName))
            {
                // Generate a new GUID for the cookie value
                var cookieValue = Guid.NewGuid().ToString();

                // Create cookie options
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Non-secure as requested
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(1) // Set expiration to 1 year
                };

                // Add the cookie to the response
                context.Response.Cookies.Append(CookieName, cookieValue, cookieOptions);
            }

            // Continue to the next middleware in the pipeline
            await _next(context);
        }
    }
}
