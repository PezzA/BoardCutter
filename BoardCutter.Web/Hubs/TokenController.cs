using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Security;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMemberManager _membershipHelper;

    public AuthController(IMemberManager membershipHelper)
    {
        _membershipHelper = membershipHelper;
    }

    [HttpGet("token")]
    public async Task<IActionResult> GetToken()
    {
        var member = await _membershipHelper.GetCurrentMemberAsync();

        if (member != null)
        {
            var username = member.Name;  // Or use member.Email or other identifying info
            var token = GenerateJwtToken(username);
            return Ok(new { token });
        }

        return Unauthorized("User is not logged in");
    }

    private string GenerateJwtToken(string username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qweqweqweqweqweqweqweqweqweqweqweqwe"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "yourIssuer",
            audience: "yourAudience",
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}