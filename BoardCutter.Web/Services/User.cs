using System.Security.Claims;

namespace BoardCutter.Web.Services;

public static class UserExtensions
{
    public static string GetUserId(IEnumerable<Claim> claims)
    {
        foreach (var claim in claims)
        {
            if (claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            {
                return claim.Value;
            }
        }

        return string.Empty;
    }
}