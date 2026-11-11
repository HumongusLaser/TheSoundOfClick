using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace TheSoundOfClick;

public class PasswordResetService(
    UserService userService,
    PasswordResetRepository resetRepository,
    PasswordResetEmailSender emailSender,
    HashingService hashingService,
    ValidationService validationService,
    IConfiguration configuration)
{
    public async Task RequestResetAsync(string email)
    {
        if (!validationService.IsValidEmail(email))
        {
            throw new InvalidOperationException("Indtast en gyldig e-mailadresse.");
        }

        User user;
        try
        {
            user = await userService.GetByEmail(email.Trim());
        }
        catch (NotFoundException)
        {
            return;
        }

        string token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        string tokenHash = HashToken(token);
        await resetRepository.CreateTokenAsync(user.ID, tokenHash);

        try
        {
            string baseUrl = configuration["PasswordReset:BaseUrl"]
                ?? throw new InvalidOperationException("Password reset base URL is not configured.");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException("Password reset base URL must be an absolute HTTP or HTTPS URL.");
            }

            string resetUrl = new Uri(
                baseUri,
                $"reset-password?token={Uri.EscapeDataString(token)}").ToString();
            await emailSender.SendResetLinkAsync(user.Email, resetUrl);
        }
        catch
        {
            await resetRepository.DeleteTokenAsync(tokenHash);
            throw;
        }
    }

    public async Task ResetPasswordAsync(
        string token,
        string newPassword,
        string confirmNewPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmNewPassword);

        if (!validationService.IsValidPassword(newPassword))
        {
            throw new InvalidOperationException("Den nye adgangskode opfylder ikke kravene.");
        }

        if (!string.Equals(newPassword, confirmNewPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("De to adgangskoder er ikke ens.");
        }

        string passwordHash = hashingService.HashPassword(newPassword);
        bool reset = await resetRepository.ResetPasswordAsync(HashToken(token), passwordHash);
        if (!reset)
        {
            throw new InvalidOperationException("Linket er ugyldigt, udløbet eller allerede brugt. Bed om et nyt nulstillingslink.");
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
