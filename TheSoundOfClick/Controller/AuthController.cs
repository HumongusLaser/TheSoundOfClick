using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace TheSoundOfClick;

// [ApiController]
// [Route("api/auth")]
// public class AuthController : ControllerBase
// {
//     [HttpPost("signin")]
//     public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
//     {
//         var claims = new List<Claim>
//         {
//             new(ClaimTypes.Email, request.Email),
//             new(ClaimTypes.Role, request.Role)
//         };
//         var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
//         var principal = new ClaimsPrincipal(identity);

//         await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
//         return Ok();
//     }
// }

[ApiController]
[Route("api/auth")]
[IgnoreAntiforgeryToken] // ← Critical: skip validation for this API endpoint
public class AuthController : ControllerBase
{
    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, request.Email),
            new(ClaimTypes.Role, request.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok();
    }
}

public class SignInRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}