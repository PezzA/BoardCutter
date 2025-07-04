using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BoardCutter.Web.Authentication
{
    public class BoardCutterAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "BoardCutter";
        public string CookieName { get; set; } = "BoardCutter";
    }

    public class BoardCutterAuthenticationHandler : AuthenticationHandler<BoardCutterAuthenticationOptions>
    {
        public BoardCutterAuthenticationHandler(IOptionsMonitor<BoardCutterAuthenticationOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the BoardCutter cookie exists
            if (!Request.Cookies.TryGetValue(Options.CookieName, out var cookieValue) || string.IsNullOrEmpty(cookieValue))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Validate the cookie value is a valid GUID
            if (!Guid.TryParse(cookieValue, out var userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid BoardCutter cookie format"));
            }

            // Create claims for the anonymous user
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, $"Anonymous-{userId.ToString()[..8]}"), // First 8 chars of GUID for display
                new Claim("BoardCutterId", userId.ToString()),
                new Claim("AuthenticationType", "Anonymous")
            };

            var identity = new ClaimsIdentity(claims, Options.CookieName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
