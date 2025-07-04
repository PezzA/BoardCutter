using BoardCutter.Web.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace BoardCutter.Web.Extensions
{
    public static class AuthenticationExtensions
    {
        public static AuthenticationBuilder AddBoardCutterAuthentication(this IServiceCollection services)
        {
            return services.AddAuthentication()
                .AddScheme<BoardCutterAuthenticationOptions, BoardCutterAuthenticationHandler>(
                    BoardCutterAuthenticationOptions.DefaultScheme, options => { });
        }
    }
}
