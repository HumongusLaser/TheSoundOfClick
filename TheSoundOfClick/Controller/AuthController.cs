using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Data.Common;
using System.Data.SqlTypes;

namespace TheSoundOfClick;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("signin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn([FromForm] SignInRequest request, [FromServices] UserService userService)
    {
        User user;
        try
        {
            user = await userService.GetByEmailWithAuth(request.Email, request.Password);
        }
        catch (NotFoundException)
        {
            return LocalRedirect("/login?error=invalid");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect("/");
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        [FromForm] RegistrationRequest request,
        [FromServices] UserService userService,
        [FromServices] HashingService hashingService,
        [FromServices] ILogger<AuthController> logger)
    {
        var validationService = new ValidationService();

        if (!validationService.IsValidName(request.FirstName))
            return LocalRedirect("/register?error=first-name");
        if (!validationService.IsValidName(request.LastName))
            return LocalRedirect("/register?error=last-name");
        if (!validationService.IsValidEmail(request.Email))
            return LocalRedirect("/register?error=email");
        if (!validationService.IsValidAddress(request.StreetAddress))
            return LocalRedirect("/register?error=address");
        if (!validationService.IsValidPostalCode(request.PostalCode))
            return LocalRedirect("/register?error=postal-code");
        if (!validationService.IsValidCity(request.City))
            return LocalRedirect("/register?error=city");
        if (!validationService.IsValidPhoneNumber(request.PhoneNumber))
            return LocalRedirect("/register?error=phone");
        if (!validationService.IsValidPassword(request.Password))
            return LocalRedirect("/register?error=password");
        if (request.Password != request.ConfirmPassword)
            return LocalRedirect("/register?error=password-mismatch");

        DateTime now = DateTime.UtcNow;
        var user = new User
        {
            RegisterDate = new DateOnly(now.Year, now.Month, now.Day),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            StreetAddress = request.StreetAddress,
            Apartment = string.IsNullOrWhiteSpace(request.Apartment) ? null : request.Apartment,
            PostalCode = request.PostalCode,
            City = request.City,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Customer,
            DiscountRole = DiscountRole.Customer
        };
        var userLogin = new UserLogin
        {
            PhcString = hashingService.HashPassword(request.Password)
        };

        try
        {
            if (await userService.EmailExistsAsync(request.Email))
                return LocalRedirect("/register?error=email-taken");

            await userService.CreateAsync(user, userLogin);
        }
        catch (Exception ex) when (ex is DbException or SqlNullValueException or NotFoundException)
        {
            logger.LogError(ex, "Could not create a registered user.");
            return LocalRedirect("/register?error=failed");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect("/");
    }
}

public class SignInRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegistrationRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string StreetAddress { get; set; } = "";
    public string? Apartment { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}